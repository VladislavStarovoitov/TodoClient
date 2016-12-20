using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Newtonsoft.Json;
using ToDoClient.Models;
using DAL.Repositories;
using DAL.ORM;
using todoclient.Infrastructure;
using System.Threading.Tasks;
using todoclient.Services;
using System.Threading;

namespace ToDoClient.Services
{
    /// <summary>
    /// Works with ToDo backend.
    /// </summary>
    public class ToDoService
    {
        private static List<Message> listOfChanges = new List<Message>();

        /// <summary>
        /// The service URL.
        /// </summary>
        private static readonly string serviceApiUrl = ConfigurationManager.AppSettings["ToDoServiceUrl"];

        /// <summary>
        /// The url for getting all todos.
        /// </summary>
        private const string GetAllUrl = "ToDos?userId={0}";

        /// <summary>
        /// The url for updating a todo.
        /// </summary>
        private const string UpdateUrl = "ToDos";

        /// <summary>
        /// The url for a todo's creation.
        /// </summary>
        private const string CreateUrl = "ToDos";

        /// <summary>
        /// The url for a todo's deletion.
        /// </summary>
        private const string DeleteUrl = "ToDos/{0}";

        private readonly HttpClient httpClient;

        private readonly ToDoRepository repository;

        private static object locker = new object();

        private static List<IdInfo> idPull = new List<IdInfo>();

        static ToDoService()
        {
            foreach (var item in new ToDoRepository(new ToDoContext()).GetAll())
            {
                idPull.Add(new IdInfo() { DbId = item.Id, AzureId = item.AzureId, Position = idPull.Count });
            }

            Task.Run(() => WorkWithQueue());
        }

        /// <summary>
        /// Creates the service.
        /// </summary>
        public ToDoService()
        {
            this.repository = new ToDoRepository(new ToDoContext());
        }

        /// <summary>
        /// Gets all todos for the user.
        /// </summary>
        /// <param name="userId">The User Id.</param>
        /// <returns>The list of todos.</returns>
        public IEnumerable<ToDoItemViewModel> GetItems(int userId)
        {
            var result = repository.GetAll();
            return result.Select(t => t.ToToDoViewModel());
        }

        /// <summary>
        /// Creates a todo. UserId is taken from the model.
        /// </summary>
        /// <param name="item">The todo to create.</param>
        public void CreateItem(ToDoItemViewModel item)
        {
            var toDo = repository.Create(item.ToToDoDal());
            idPull.Add(new IdInfo { DbId = toDo.Id, Position = idPull.Count });
            listOfChanges.Add(new Message(toDo.ToToDoViewModel(), Operation.Create));
        }

        /// <summary>
        /// Updates a todo.
        /// </summary>
        /// <param name="item">The todo to update.</param>
        public void UpdateItem(ToDoItemViewModel item)
        {
            repository.Update(item.ToToDoDal());

            listOfChanges.RemoveAll(i => i.ToDo.ToDoId == item.ToDoId && i.Operation != Operation.Create);
            
            listOfChanges.Add(new Message(item, Operation.Update));
        }

        /// <summary>
        /// Deletes a todo.
        /// </summary>
        /// <param name="id">The todo Id to delete.</param>
        public void DeleteItem(int id)
        {
            var item = repository.Delete(id).ToToDoViewModel();

            listOfChanges.RemoveAll(i => i.ToDo.ToDoId == id && i.Operation != Operation.Create);

            listOfChanges.Add(new Message(item, Operation.Delete));
        }

        private static void WorkWithQueue()
        {
            while (true)
            {
                if (listOfChanges.Count > 0)
                {
                    var action = listOfChanges[0];
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    var idLock = idPull.FirstOrDefault(i => (int)i.DbId == action.ToDo.ToDoId);
                    Task.Run(() =>
                    {
                        lock (idLock)
                        {
                            switch (action.Operation)
                            {
                                case Operation.Create:
                                    httpClient.PostAsJsonAsync(serviceApiUrl + CreateUrl, action.ToDo).Result.EnsureSuccessStatusCode();
                                    var dataAsString = httpClient.GetStringAsync(string.Format(serviceApiUrl + GetAllUrl, action.ToDo.UserId)).Result;
                                    var list = JsonConvert.DeserializeObject<IEnumerable<ToDoItemViewModel>>(dataAsString).OrderBy(i => i.ToDoId).ToList();

                                    //foreach (var item in list)
                                    //{
                                    //    httpClient.DeleteAsync(string.Format(serviceApiUrl + DeleteUrl, item.ToDoId)).Result.EnsureSuccessStatusCode();
                                    //}

                                    idLock.AzureId = list[idPull.IndexOf(idLock)].ToDoId;
                                    
                                    break;

                                case Operation.Update:
                                    action.ToDo.ToDoId = (int)idLock.AzureId;
                                    httpClient.PutAsJsonAsync(serviceApiUrl + UpdateUrl, action.ToDo).Result.EnsureSuccessStatusCode();
                                   
                                    break;

                                case Operation.Delete:
                                    httpClient.DeleteAsync(string.Format(serviceApiUrl + DeleteUrl, (int)idPull.FirstOrDefault(i => (int)i.DbId == action.ToDo.ToDoId).AzureId)).Result.EnsureSuccessStatusCode();
                                    idPull.RemoveAll(i => (int)i.DbId == action.ToDo.ToDoId);
                                    break;
                            }
                        }
                    });
                    listOfChanges.Remove(action);
                }
            }
        }
    }
}