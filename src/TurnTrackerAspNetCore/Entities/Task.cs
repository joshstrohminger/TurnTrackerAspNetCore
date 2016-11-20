﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TurnTrackerAspNetCore.Entities
{
    public class Task
    {
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public bool TeamBased { get; set; }
    }
}
