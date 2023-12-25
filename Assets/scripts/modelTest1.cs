using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

public class modelTest1 : MonoBehaviour
{

    public Texture2D poseImg;
    public NNModel modelAsset;
    private Model m_RuntimeModel;


    void Start()
    {   
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        var worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel);
        Tensor input = new Tensor(poseImg, 4);
        worker.Execute(input);

        var output = worker.PeekOutput();
        Console.Write(output);
    }    


    // Update is called once per frame
    void Update()
    {
        
    }
}
