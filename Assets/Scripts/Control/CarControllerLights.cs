using System.Collections;
using UnityEngine;
using static OverdriveServer.NetStructures;

class LightStructure
{
    float headEndTime, engineEndTime, tailEndTime;
    public bool headOn, engineCustom, tailOn;
    int headPriority, enginePriority, tailPriority;
    public void Reset()
    {
        headEndTime = Time.time + 0.25f; engineEndTime = Time.time + 0.25f; tailEndTime = Time.time + 0.25f;
        headOn = false; engineCustom = false; tailOn = false;
        headPriority = 0; enginePriority = 0; tailPriority = 0;
    }
    public bool SetHeadTime(float duration, int priority){
        if(priority < headPriority){ return false; } // Don't override if lower priority than current effect
        headEndTime = Time.time + duration;
        headOn = true;
        headPriority = priority;
        return true;
    }
    public bool SetEngineTime(float duration, int priority) {
        if(priority < enginePriority){ return false; } // Don't override if lower priority than current effect
        engineEndTime = Time.time + duration;
        engineCustom = true;
        enginePriority = priority;
        return true;
    }
    public bool SetTailTime(float duration, int priority){
        if(priority < tailPriority){ return false; } // Don't override if lower priority than current effect
        tailEndTime = Time.time + duration;
        tailOn = true;
        tailPriority = priority;
        return true;
    }
    public (bool head, bool engineCustom, bool tail) CheckExpired(){
        bool h = false, e = false, t = false;
        if(Time.time > headEndTime && headOn){ h = true; headOn = false; headPriority = 0; }
        if(Time.time > engineEndTime && engineCustom){ e = true; engineCustom = false; enginePriority = 0; }
        if(Time.time > tailEndTime && tailOn){ t = true; tailOn = false; tailPriority = 0; }
        return (h,e,t); //true if effect expired and light should be cleared, false if effect still active
    }
}

public partial class CarController : MonoBehaviour
{
    // Fields owned by this partial
    LightStructure lights = new LightStructure();

#region CAR LIGHT CONTROL
    void SetHeadLights(LightData[] lightData, float duration, int priority = 1){
        bool valid = lights.SetHeadTime(duration, priority);
        if(!valid){ return; }
        UCarData carData = SR.io.GetCarFromID(carID);
        if(carData == null) return;
        SR.io.SetCarColoursComplex(carData, lightData);
    }
    IEnumerator SetHeadLightsDelayed(LightData[] lightData, float duration, float delay, int priority = 1){
        yield return new WaitForSeconds(delay);
        SetHeadLights(lightData, duration, priority);
    }
    void SetEngineLight(LightData[] lightData, float duration, int priority = 1){
        bool valid = lights.SetEngineTime(duration, priority);
        if(!valid){ return; }
        UCarData carData = SR.io.GetCarFromID(carID);
        if(carData == null) return;
        SR.io.SetCarColoursComplex(carData, lightData);
    }
    IEnumerator SetEngineLightDelayed(LightData[] lightData, float duration, float delay, int priority = 1){
        yield return new WaitForSeconds(delay);
        SetEngineLight(lightData, duration, priority);
    }
    void SetTailEffect(LightEffect effect, int startStrength, int endStrength, int cyclesPer10Seconds, float duration, int priority = 1) {
        bool valid = lights.SetTailTime(duration, priority);
        if(!valid){ return; }
        UCarData carData = SR.io.GetCarFromID(carID);
        if(carData == null) return;
        LightData[] lightarr = new LightData[1];
        lightarr[0] = new LightData{ channel = LightChannel.TAIL, effect = effect, startStrength = startStrength, endStrength = endStrength, cyclesPer10Seconds = cyclesPer10Seconds };
        SR.io.SetCarColoursComplex(carData, lightarr);
    }
    IEnumerator SetTailEffectDelayed(LightEffect effect, int startStrength, int endStrength, int cyclesPer10Seconds, float duration, float delay, int priority = 1) {
        yield return new WaitForSeconds(delay);
        SetTailEffect(effect, startStrength, endStrength, cyclesPer10Seconds, duration, priority);
    }
    /// <summary>
    /// Check if the lights should be cleared based on the end time, and clear them if needed.
    /// </summary>
    void CheckAndClearLights()
    {
        UCarData carData = SR.io.GetCarFromID(carID);
        if(carData == null) return;
        LightData[] lightArr;
        (bool head,bool engine,bool tail) = lights.CheckExpired();
        int channels = 0;
        if(tail){ channels++; }
        if(head){ channels += 2; }
        if(channels == 0 && !engine){ return; }
        lightArr = new LightData[channels];
        if(channels == 1)
        { lightArr[0] = LightData.ClearFor(LightChannel.TAIL); }
        else if(channels == 2){ 
            lightArr[0] = LightData.ClearFor(LightChannel.FRONT_GREEN);
            lightArr[1] = LightData.ClearFor(LightChannel.FRONT_RED);
        }
        else if(channels == 3){
            lightArr[0] = LightData.ClearFor(LightChannel.TAIL);
            lightArr[1] = LightData.ClearFor(LightChannel.FRONT_GREEN);
            lightArr[2] = LightData.ClearFor(LightChannel.FRONT_RED);
        }
        SR.io.SetCarColoursComplex(carData, lightArr);
        if (engine)
        {
            int r = Mathf.RoundToInt(playerColor.r * 14f);
            int g = Mathf.RoundToInt(playerColor.g * 14f);
            int b = Mathf.RoundToInt(playerColor.b * 14f);
            int endR = Mathf.RoundToInt(r * 0.8f);
            int endG = Mathf.RoundToInt(g * 0.8f);
            int endB = Mathf.RoundToInt(b * 0.8f);
            lightArr = new LightData[3]{
                new LightData{ channel = LightChannel.RED, effect = LightEffect.THROB, startStrength = r, endStrength = endR, cyclesPer10Seconds = 4 },
                new LightData{ channel = LightChannel.GREEN, effect = LightEffect.THROB, startStrength = g, endStrength = endG, cyclesPer10Seconds = 4 },
                new LightData{ channel = LightChannel.BLUE, effect = LightEffect.THROB, startStrength = b, endStrength = endB, cyclesPer10Seconds = 4 }
            };
            SR.io.SetCarColoursComplex(carData, lightArr);
        }

    }
#endregion
}
