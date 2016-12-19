using System;
using System.ComponentModel.DataAnnotations;

namespace TurnTrackerAspNetCore.ViewModels.Turn
{
    public class EditTurnViewModel
    {
        [DataType(DataType.DateTime)]
        public DateTimeOffset Taken { get; set; }
    }
}
