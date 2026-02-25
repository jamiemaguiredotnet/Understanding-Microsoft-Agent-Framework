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
        [Description("Calculates the time taken to reach target weight")]
        public static DateTime GetTimeToReachWeight([Description("Persons current weight")] int currentKilogram, int targetKilograms)
        {
            // Assume a safe weight loss of 0.5 kg per week
            const double weeklyWeightLossKg = 0.5;
            
            if (currentKilogram <= targetKilograms)
            {
                throw new ArgumentException("Target weight must be less than current weight.");
            }
            
            int totalWeightToLose = currentKilogram - targetKilograms;
            int weeksNeeded = (int)Math.Ceiling(totalWeightToLose / weeklyWeightLossKg);
            
            return DateTime.Now.AddDays(weeksNeeded * 7);
        }
    }
}
