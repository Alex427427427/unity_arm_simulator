using UnityEngine;
using System;
using System.Collections;
public class colour_timer : MonoBehaviour
{
    public GameObject[] models;
    // All time-based variables are in seconds
    public int delay_time;
    public float flash_time;
    public float stop_time; 
    private Color pale_red = new Color(1f, 0.345f, 0.345f, 1f);
    void Start()
    {       
        StartCoroutine(colour_shift());
    }
    IEnumerator colour_shift()
    {
        
    
        //Wait for the specified delay time before running
        yield return new WaitForSeconds(delay_time);
        red();
    
        //Creates a time variable
        var start_time = DateTime.UtcNow;
    
        // while loop runs until stop_time has been reached.
        while (DateTime.UtcNow - start_time < TimeSpan.FromSeconds(stop_time))
        {
            yield return new WaitForSeconds(flash_time);
            pale();
            yield return new WaitForSeconds(flash_time);
            red();
        }
    
        white(); // Reset model back to white
    }

    // red() turns all arm models red
    void red()
    {
        foreach(GameObject model in models)
        {
            model.GetComponent<Renderer>().material.color = Color.red;
        }
    }
    
    // pale() turns all arm models pale_red
    void pale()
    {
        foreach(GameObject model in models)
        {
            model.GetComponent<Renderer>().material.color = pale_red;
        }
    }
    
    void white()
    {
        foreach(GameObject model in models)
        {
            model.GetComponent<Renderer>().material.color = Color.white;
        }
    }
}