﻿using System.Collections.Generic;
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
        private readonly string serviceApiUrl = ConfigurationManager.AppSettings["ToDoServiceUrl"];

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

        /// <summary>
        /// Creates the service.
        /// </summary>
        public ToDoService()
        {
            this.repository = new ToDoRepository(new ToDoContext());
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Gets all todos for the user.
        /// </summary>
        /// <param name="userId">The User Id.</param>
        /// <returns>The list of todos.</returns>
        public IEnumerable<ToDoItemViewModel> GetItems(int userId)
        {
            var result = repository.GetAll();
            Task.Run(() => GetAll(userId, result.OrderBy(t => t.Id)));
            return result.Select(t => t.ToToDoViewModel());
        }

        /// <summary>
        /// Creates a todo. UserId is taken from the model.
        /// </summary>
        /// <param name="item">The todo to create.</param>
        public void CreateItem(ToDoItemViewModel item)
        {
            repository.Create(item.ToToDoDal());
            httpClient.PostAsJsonAsync(serviceApiUrl + CreateUrl, item);
        }

        /// <summary>
        /// Updates a todo.
        /// </summary>
        /// <param name="item">The todo to update.</param>
        public void UpdateItem(ToDoItemViewModel item)
        {
            if (repository.Update(item.ToToDoDal()))
            {
                var change = listOfChanges.SingleOrDefault(i => i.ToDo.ToDoId == item.ToDoId);

                if (ReferenceEquals(change, null))
                {
                    listOfChanges.Add(new Message(item, Operation.Update));
                }
                else
                {
                    change.Operation = Operation.Update;
                    change.ToDo = item;
                }
            }
        }

        /// <summary>
        /// Deletes a todo.
        /// </summary>
        /// <param name="id">The todo Id to delete.</param>
        public void DeleteItem(int id)
        {
            if (repository.Delete(id))
            {
                var change = listOfChanges.SingleOrDefault(i => i.ToDo.ToDoId == id);

                if (ReferenceEquals(change, null))
                {
                    listOfChanges.Add(new Message(id, Operation.Update));
                }
                else
                {
                    change.Operation = Operation.Update;
                    change.Id = id;
                }
            }
        }

        private void GetAll(int userId, IEnumerable<ToDo> result)
        {
            var dataAsString = httpClient.GetStringAsync(string.Format(serviceApiUrl + GetAllUrl, userId)).Result;
            var list = JsonConvert.DeserializeObject<IEnumerable<ToDoItemViewModel>>(dataAsString).OrderBy(i => i.ToDoId).ToList();

            for (int i = 0; i < result.Count(); i++)
            {
                var toDoFromDb = result.ToList()[i];
                if (ReferenceEquals(toDoFromDb.AzureId, null))
                {
                    toDoFromDb.AzureId = list[i].ToDoId;
                } 
            }

            lock (locker)
            {
                foreach (var change in listOfChanges)
                {
                    switch (change.Operation)
                    {
                        case Operation.Update:
                            httpClient.PutAsJsonAsync(serviceApiUrl + UpdateUrl, change.ToDo);
                            break;
                        case Operation.Delete:
                            httpClient.DeleteAsync(string.Format(serviceApiUrl + DeleteUrl, change.Id));
                            break;
                    }
                }
            }
        }
    }
}