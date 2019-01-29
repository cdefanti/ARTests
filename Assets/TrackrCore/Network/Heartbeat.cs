using System;
using System.Timers;

using UnityEngine;

public class Heartbeat {

    public int period {get; set;}
    float adjustment {get; set;}

    public System.Timers.Timer aTimer;
    // add a delegate
    public Heartbeat(int perio, float adj) {
        period = perio;
        adjustment = adj;
        //aTimer = new System.Timers.Timer(period * adjustment);
        //resetTimer();
    }

    public void resetTimer() {
        // Create a timer with a two second interval.
        aTimer = new System.Timers.Timer(period * adjustment);
        // Hook up the Elapsed event for the timer. 
       // aTimer.Elapsed += OnTimedEvent;
        aTimer.AutoReset = true;
        aTimer.Enabled = true;
    }
    
    public void pause() {
        aTimer.Enabled = false;
    }

    public void stop() {
        aTimer.Stop();
        aTimer.Enabled = false;
        aTimer.AutoReset = false;
        aTimer.Start();
    }

    private void OnTimedEvent(object source, ElapsedEventArgs e) {
        Debug.Log("The Elapsed event was raised at {0:HH:mm:ss.fff}" + e.SignalTime);
    }
}