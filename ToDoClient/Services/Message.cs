using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ToDoClient.Models;

namespace todoclient.Services
{
    public enum Operation
    {
        Update = 0,
        Delete = 1,
        Create = 2
    }

    public class Message
    {
        public Operation Operation { get; set; }

        public ToDoItemViewModel ToDo { get; set; }

        public Message(ToDoItemViewModel toDo, Operation operation)
        {
            this.ToDo = toDo;
            this.Operation = operation;
        }
    }
}