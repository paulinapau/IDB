﻿using System;
namespace Org.Quickstart.API.Models
{
    public class ProfileRequest
    {
        public String FirstNameSearch { get; set; }
        public int Limit { get; set; }
        public int Skip { get; set; }
    }
}