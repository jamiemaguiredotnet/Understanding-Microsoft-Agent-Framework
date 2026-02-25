using Agent_Framework_3_Agent_as_Funciton_Tools.FunctionTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_Framework_3_Agent_as_Funciton_Tools.Agents
{
    public class PersonalTrainerAgent
    {
        [Description("Determines the next available date for a booking")]
        public static DateTime GetNextAvailableDate([Description("The date to start searching from")] DateTime startDate)
        {
            // get the next available date
            return BookingService.GetNextAvailableDate(startDate);
        }


        [Description("Allows the user to make a booking.")]
        public static string BookAppointment([Description("The date of the appointment")] DateTime date)
        {
            return BookingService.BookAppointment(date);
        }


        [Description("Cancels a booking.")]
        public static string CancelAppointment([Description("The date of the appointment to cancel")] DateTime date)
        {
            return BookingService.CancelAppointment(date);
        }

        // list all booked appointments
        [Description("Lists all booked appointments.")]
        public static List<DateTime> ListBookedAppointments()
        {
            return BookingService.ListBookedAppointments();
        }
    }
}
