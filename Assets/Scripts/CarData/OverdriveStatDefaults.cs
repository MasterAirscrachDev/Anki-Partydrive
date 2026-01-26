using UnityEngine;
using static OverdriveServer.NetStructures;

public class OverdriveStatDefaults
{
    //If the player wishes to use base stat points for unique cars (not all balanced equally)

    public class StatTable
    {
        public int speedModPoints;
        public int steerModPoints;
        public int boostModPoints;
        public int maxEnergyModPoints;
        public int energyRechargeModPoints;
        public StatTable(int speed, int steer, int boost, int maxEnergy, int energyRecharge)
        {
            speedModPoints = speed;
            steerModPoints = steer;
            boostModPoints = boost;
            maxEnergyModPoints = maxEnergy;
            energyRechargeModPoints = energyRecharge;
        }
    }
    public static StatTable GetDefaultsForCarOverdrive(ModelName carName)
    {
        //every car gets max 8 points to distribute among stats
        switch (carName) {
            case ModelName.Kourai:
            case ModelName.Boson:
            case ModelName.Rho:
            case ModelName.Katal:
            case ModelName.Hadion:
            case ModelName.Spektrix:
            case ModelName.Corax:
               return new StatTable(0, 0, 0, 0, 0); // drive cars are balanced
            //Speed cars
            case ModelName.Skull:
            case ModelName.Thermo:
            case ModelName.IceWave:
            case ModelName.Dynamo:
                return new StatTable(5, 1, 0, 0, 2); //8 points
            //Damage Cars
            case ModelName.Groundshock:
            case ModelName.Nuke:
            case ModelName.NukePhantom:
            case ModelName.Mammoth:
                return new StatTable(1, 0, 2, 2, 4); //8 points
            //Tank Cars
            case ModelName.Guardian:
            case ModelName.Bigbang:
                return new StatTable(0, 0, 0, 6, 2); //8 points
            //Trucks
            case ModelName.Freewheel:
            case ModelName.x52:
            case ModelName.x52Ice:
                return new StatTable(-3, 2, -1, 4, 4); //6 points
            default:
                return new StatTable(0, 0, 0, 0, 0); //default no changes
        }
    }
    public static StatTable GetDefaultsForCarBalanced(ModelName carName)
    {
        //every car gets max 8 points to distribute among stats
        switch (carName) {
            case ModelName.Freewheel:
            case ModelName.x52:
            case ModelName.x52Ice:
                return new StatTable(-3, 2, -1, 4, 4); //6 points
            default:
                return new StatTable(2, 2, 2, 1, 1); //default no changes
        }
    }
}
