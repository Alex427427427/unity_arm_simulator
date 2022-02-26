using UnityEngine;
using System.Collections;
public class colour_timer : MonoBehaviour
{
    public GameObject model;
    public int delay_time;
   void Start()
   {
       StartCoroutine(colour_shift());
   }
   IEnumerator colour_shift()
   {
       //Wait for the specified delay time before running
       yield return new WaitForSeconds(delay_time);
       //Get the Renderer component from the model
       var modelRenderer = model.GetComponent<Renderer>();
       //Call SetColor using the shader property name "_Color" and setting the color to red
       modelRenderer.material.SetColor("_Color", Color.red);
   }
}