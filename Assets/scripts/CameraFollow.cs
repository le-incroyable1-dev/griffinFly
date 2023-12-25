using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.1f;
    public Vector3 locationOffset;
    public Vector3 rotationOffset;

    private void Start(){

        //if no target was assigned set defaults
        
        if(target == null){
            Transform default_target = gameObject.transform;
            target = default_target;
            locationOffset = new Vector3(0,0,0);
            rotationOffset = new Vector3(0,0,0);
        }
    }

    private void Update()
    {

        //get the desired position and rotation
        //slowly transition to the desired transform state according to the smoothSpeed

        Vector3 desiredPosition = target.position + target.rotation * locationOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        Quaternion desiredrotation = target.rotation * Quaternion.Euler(rotationOffset);
        Quaternion smoothedrotation = Quaternion.Lerp(transform.rotation, desiredrotation, smoothSpeed);
        transform.rotation = smoothedrotation;
    }
}
