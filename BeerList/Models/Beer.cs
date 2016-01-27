using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BeersList.Models
{
    public class Beer
    {
        public int BeerId { get; set; }
        public string Name { get; set; }
        public string Brewery { get; set; }
        public string Style { get; set; }
    }
}