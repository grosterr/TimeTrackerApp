using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace WpfApp1.Models
{
    [Table("Users")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Unique]
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityQuestion { get; set; }
        public string SecurityAnswerHash { get; set; }
    }
}
