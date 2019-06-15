using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBot.Models
{
    public class Clase
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public List<Tutoria> tutorias { get; set; }
    }
}
