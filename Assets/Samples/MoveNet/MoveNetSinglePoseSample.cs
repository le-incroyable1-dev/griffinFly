using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TensorFlowLite;
using TensorFlowLite.MoveNet;
using System;
using Unity.Barracuda;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using System.Collections;
using UI = UnityEngine.UI;

[RequireComponent(typeof(WebCamInput))]
public class MoveNetSinglePoseSample : MonoBehaviour
{
    [SerializeField]
    MoveNetSinglePose.Options options = default;
    [SerializeField]
    private RectTransform cameraView = null;
    [SerializeField]
    private bool runBackground = false;
    public float threshold = 0.8f;
    public powerupController powerupControl;
    private MoveNetSinglePose moveNet;
    private MoveNetPose pose;
    private MoveNetDrawer drawer;
    private UniTask<bool> task;
    private CancellationToken cancellationToken;
    public float leftShoulderHt;
    public float rightShoulderHt;
    public float leftElbowHt;
    public float rightElboxHt;
    public float noseHt;
    public float ht_diff = 0;
    public NNModel yoloModelAsset;
    private Model m_RuntimeModel;
    private IWorker worker;
    public RawImage displayImage;
    public Texture2D sampleTex;
    public string shapeDetected;
    public float yoloTimer;
    private readonly string[] _labels = {
        "circle",
        "rectangle",
        "square",
        "triangle"
        };
    


    private void Start()
    {
        yoloTimer = 3f;  //initialize the timer as yolo would be run every x seconds
        threshold = 0.8f;
        moveNet = new MoveNetSinglePose(options);
        //drawer = new MoveNetDrawer(Camera.main, cameraView);
        m_RuntimeModel = ModelLoader.Load(yoloModelAsset, false);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel);
        cancellationToken = this.GetCancellationTokenOnDestroy();
        var webCamInput = GetComponent<WebCamInput>();
        webCamInput.OnTextureUpdate.AddListener(OnTextureUpdate);
        webCamInput.OnTextureUpdate.AddListener(executeYolo);
    }

    private void OnDestroy()
    {
        var webCamInput = GetComponent<WebCamInput>();
        webCamInput.OnTextureUpdate.RemoveListener(OnTextureUpdate);
        moveNet?.Dispose();
        drawer?.Dispose();
    }

    private void Update()
    {

        if(yoloTimer > 0f){
            yoloTimer -= Time.deltaTime;
        }

        if (pose != null)
        {
            //drawer.DrawPose(pose, threshold);

            leftShoulderHt = pose[5].y;
            rightShoulderHt = pose[6].y;
            ht_diff = leftShoulderHt - rightShoulderHt;

            leftElbowHt = pose[7].y;
            rightElboxHt = pose[8].y;
            noseHt = pose[0].y;
        }
    }

    private void OnTextureUpdate(Texture texture)
    {
        if (runBackground)
        {
            if (task.Status.IsCompleted())
            {
                task = InvokeAsync(texture);
            }
        }
        else
        {
            //curWebcamCapture = texture;
            Invoke(texture);
        }
    }

    private void Invoke(Texture texture)
    {
        moveNet.Invoke(texture);
        pose = moveNet.GetResult();
    }

    private async UniTask<bool> InvokeAsync(Texture texture)
    {
        await moveNet.InvokeAsync(texture, cancellationToken);
        pose = moveNet.GetResult();
        return true;
    }

    private List<DetectionResult> ParseOutputs(Tensor output0, float iouThres) {
        int outputWidth = output0.shape.width;
        
        List<DetectionResult> candidateDitects = new List<DetectionResult>();
        List<DetectionResult> ditects = new List<DetectionResult>();

        for (int i = 0; i < outputWidth; i++) {
            var result = new DetectionResult(output0, i);
            if (result.score < threshold) {
                continue;
            }
            candidateDitects.Add(result);
        }

        while (candidateDitects.Count > 0) {
            int idx = 0;
            float maxScore = 0.0f;
            for (int i = 0; i < candidateDitects.Count; i++) {
                if (candidateDitects[i].score > maxScore) {
                    idx = i;
                    maxScore = candidateDitects[i].score;
                }
            }

            var cand = candidateDitects[idx];
            candidateDitects.RemoveAt(idx);
            ditects.Add(cand);

            List<int> deletes = new List<int>();
            for (int i = 0; i < candidateDitects.Count; i++) {
                float iou = Iou(cand, candidateDitects[i]);
                if (iou >= iouThres) {
                    deletes.Add(i);
                }
            }
            for (int i = deletes.Count - 1; i >= 0; i--) {
                candidateDitects.RemoveAt(deletes[i]);
            }

        }

        return ditects;

    }

    public void executeYolo(Texture tex){

        Texture2D tex2d = convertTo2D(tex);
        Texture2D resized = ResizedTexture(tex2d, 640, 640);

        //for display of feed
        displayImage.texture = resized;

        if(yoloTimer > 0f) return;
        else yoloTimer = 3f;
        Debug.Log("Yolo running!");

        bool canDetect = !powerupControl.invinc && !powerupControl.speed && !powerupControl.score;
        if(!canDetect){
            return;
        }

        Tensor input = new Tensor(resized, 3);

        //Debug.Log("Input Ready");
        worker.Execute(input);
        //Debug.Log("Input Executed");
        Tensor output0 = worker.PeekOutput("output0");
        //Debug.Log("Output Retrieved, Shape : " + output0.shape);
        
        List<DetectionResult> ditects = ParseOutputs(output0, 0f);
        if(ditects.Count == 0) return;

        foreach (DetectionResult ditect in ditects) {
            Debug.Log($"{_labels[ditect.classId]}: {ditect.score:0.00}");
            shapeDetected = _labels[ditect.classId];
        }
        

        if(canDetect){
            if(shapeDetected == "square" || shapeDetected == "rectangle") powerupControl.activateSpeedPowerup();
            else if(shapeDetected == "circle") powerupControl.activateInvincibility();
            else if(shapeDetected == "triangle") powerupControl.activateScoreMultiplier();
        }

        input.Dispose();
        output0.Dispose();
    }

    private Texture2D convertTo2D(Texture mainTexture){
             Texture2D texture2D = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);
 
             RenderTexture renderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 32);
             Graphics.Blit(mainTexture, renderTexture);

             texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
             texture2D.Apply();
 
             Color[] pixels = texture2D.GetPixels();

             return texture2D;
    }

    private static Texture2D ResizedTexture(Texture2D texture, int width, int height) {
        // RenderTextureに書き込む
        var rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(texture, rt);
        // RenderTexgureから書き込む
        var preRt = RenderTexture.active;
        RenderTexture.active = rt;
        var resizedTexture = new Texture2D(width, height);
        resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizedTexture.Apply();
        RenderTexture.active = preRt;
        RenderTexture.ReleaseTemporary(rt);
        return resizedTexture;
    }

    private float Iou(DetectionResult boxA, DetectionResult boxB) {
        if ((boxA.x1 == boxB.x1) && (boxA.x2 == boxB.x2) && (boxA.y1 == boxB.y1) && (boxA.y2 == boxB.y2)) {
            return 1.0f;

        } else if (((boxA.x1 <= boxB.x1 && boxA.x2 > boxB.x1) || (boxA.x1 >= boxB.x1 && boxB.x2 > boxA.x1))
            && ((boxA.y1 <= boxB.y1 && boxA.y2 > boxB.y1) || (boxA.y1 >= boxB.y1 && boxB.y2 > boxA.y1))) {
            float intersection = (Mathf.Min(boxA.x2, boxB.x2) - Mathf.Max(boxA.x1, boxB.x1)) 
                * (Mathf.Min(boxA.y2, boxB.y2) - Mathf.Max(boxA.y1, boxB.y1));
            float union = (boxA.x2 - boxA.x1) * (boxA.y2 - boxA.y1) + (boxB.x2 - boxB.x1) * (boxB.y2 - boxB.y1) - intersection;
            return (intersection / union);
        }

        return 0.0f;
    }

    // WebCamDevice device = WebCamTexture.devices[current_cam_index];
    // tex = new WebCamTexture(device.name);
    // display.texture = tex;
    // tex.Play();
}

class DetectionResult {
    public float x1 { get; }
    public float y1 { get; }
    public float x2 { get; }
    public float y2 { get; }
    public int classId { get; }
    public float score { get; }

    public DetectionResult(Tensor t, int idx) {
        // 検出結果で得られる矩形の座標情報は0:中心x, 1:中心y、2:width, 3:height
        // 座標系を左上xy右下xyとなるよう変換
        float halfWidth = t[0, 0, idx, 2] / 2;
        float halfHeight = t[0, 0, idx, 3] / 2;
        x1 = t[0, 0, idx, 0] - halfWidth;
        y1 = t[0, 0, idx, 1] - halfHeight;
        x2 = t[0, 0, idx, 0] + halfWidth;
        y2 = t[0, 0, idx, 1] + halfHeight;

        // 残りの領域に各クラスのスコアが設定されている
        // 最大値を判定して設定
        int classes = t.shape.channels - 4;
        score = 0f;
        for (int i = 0; i < classes; i++) {
            float classScore = t[0, 0, idx, i + 4];
            if (classScore < score) {
                continue;
            }
            classId = i;
            score = classScore;
        }
    }

}
