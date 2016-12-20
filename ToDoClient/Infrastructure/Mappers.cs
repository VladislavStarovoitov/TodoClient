using DAL.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ToDoClient.Models;

namespace todoclient.Infrastructure
{
    public static class Mappers
    {
        public static ToDo ToToDoDal(this ToDoItemViewModel viewModel)
        {
            return new ToDo
            {
                Id = viewModel.ToDoId,
                Completed = viewModel.IsCompleted,
                Task = viewModel.Name,
                UserId = viewModel.UserId
            };
        }

        public static ToDoItemViewModel ToToDoViewModel(this ToDo item)
        {
            return new ToDoItemViewModel
            {
                ToDoId = item.Id,
                IsCompleted = item.Completed,
                Name = item.Task,
                UserId = item.UserId
            };
        }
    }
}