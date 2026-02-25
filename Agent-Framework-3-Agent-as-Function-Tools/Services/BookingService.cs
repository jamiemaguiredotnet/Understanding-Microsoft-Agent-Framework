using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_Framework_3_Agent_as_Funciton_Tools.FunctionTools
{
    public class BookingService
    {
        // a list of available dates the user can book
        private static List<DateTime> _available = new List<DateTime>
        {
            // add dates for the next 9 days, ignore the time part
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1,0,0,0),
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 2,0,0,0),
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 3,0,0,0),
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 4,0,0,0),
            new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 5,0,0,0)
        };

        // a list of booked dates
        private static List<DateTime> _booked = new List<DateTime>();

        public static DateTime GetNextAvailableDate(DateTime startDate)
        {
            // get the next available date
            return _available.FirstOrDefault(d => d > startDate);
        }

        public static string BookAppointment(DateTime date)
        {
            // check if the date is available, ignore the time part
            date = date.Date;

            if (_available.Contains(date))
            {
                // remove the date from the available list
                _available.Remove(date);

                // add the date to the booked list
                _booked.Add(date);

                // return a confirmation message
                return $"Appointment booked for {date.ToShortDateString()}";
            }
            else
            {
                // return an error message
                return "The selected date is not available";
            }
        }


        public static string CancelAppointment(DateTime date)
        {
            // check if the date is booked
            if (_booked.Contains(date))
            {
                // remove the date from the booked list
                _booked.Remove(date);

                // add the date back to the available list
                _available.Add(date);

                // return a confirmation message
                return $"Appointment on {date.ToShortDateString()} cancelled";
            }
            else
            {
                // return an error message
                return "The selected date is not booked";
            }
        }

        // list all booked appointments
        [Description("Lists all booked appointments.")]
        public static List<DateTime> ListBookedAppointments()
        {
            return _booked;
        }
    }
}
