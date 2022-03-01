using UnityEngine;
using System.Collections;
public class colour_timer : MonoBehaviour
{
    public GameObject[] models;
    public int delay_time;
    public float flash_time;
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
        while (true)
        {
            yield return new WaitForSeconds(flash_time);
            pale();
            yield return new WaitForSeconds(flash_time);
            red();
        }
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
}