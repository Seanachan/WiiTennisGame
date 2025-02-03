using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;
public class ReplaySystem : MonoBehaviour
{
    public Camera mainCamera;  // Reference to the main game camera
    public Camera replayCamera;  // Player 1's camera
    public RenderTexture replayRenderTexture;  // Render texture for recording
    public float delayBetweenFrames = 0.05f;
    private List<RenderTexture> frames = new List<RenderTexture>(); // Changed to RenderTexture
    private bool isRecording;
    private bool isPlaying;
    public Material replayMaterial; // Material to show replay
    private int currentWidth,currentHeight;
    public RawImage replayRawImage; 
    [SerializeField] private GameController gameController;

    void Start()
    {
        // Create render texture with explicit format and make it active
        replayRawImage.enabled=false;

        if (replayRenderTexture == null)
        {
            currentWidth = Screen.width;
            currentHeight = Screen.height;
            replayRenderTexture = new RenderTexture(currentWidth, currentHeight, 24);
            replayRenderTexture.format = RenderTextureFormat.ARGB32;
            replayRenderTexture.useMipMap = false;
            replayRenderTexture.antiAliasing = 1;
            replayRenderTexture.Create();
        }

        // Setup replay camera
        if (replayCamera != null)
        {
            replayCamera.targetTexture = replayRenderTexture;
            replayCamera.enabled = false;
            replayCamera.clearFlags = CameraClearFlags.SolidColor;
            replayCamera.backgroundColor = Color.black;
        }
        else
        {
            Debug.LogError("Replay camera not assigned!");
        }

        // Setup main camera reference
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
    }

    public void StartRecording()
    {
        Debug.Log("Starting recording...");
        frames.Clear();
        isRecording = true;
        replayCamera.enabled = true;
    }

    public void StopRecording()
    {
        isRecording = false;
        replayCamera.enabled = false;
    }

    private void LateUpdate()
    {
        if (isRecording)
        {
            RecordFrame();
        }
    }

    private void RecordFrame()
    {
        if (!isRecording || replayRenderTexture == null) return;

        // Create a new RenderTexture for this frame
        RenderTexture frameTexture = new RenderTexture(replayRenderTexture.width, replayRenderTexture.height, 24);
        
        // Render the camera to its target texture
        replayCamera.Render();
        
        // Copy the rendered frame to our new texture
        Graphics.Blit(replayRenderTexture, frameTexture);

        frames.Add(frameTexture);
        // Debug.Log($"Frame captured. Total frames: {frames.Count}");
    }

    public void StartPlayback(Controller player1, Controller player2)
    {
        // Hide server text for both players during replay
        player1.setServerText(false);
        player2.setServerText(false);

        if (!isPlaying && frames.Count > 0)
        {
            Debug.Log($"Starting playback. Frame count: {frames.Count}");
            replayRawImage.enabled = true;
            StartCoroutine(Playback(player1, player2));
        }
        else
        {
            Debug.LogWarning($"Cannot start playback. isPlaying: {isPlaying}, frames: {frames.Count}");
        }
    }

    private IEnumerator Playback(Controller player1, Controller player2)
    {
        if (frames.Count == 0)
        {
            Debug.LogWarning("No frames to play back");
            replayRawImage.enabled = false;
            yield break;
        }

        isPlaying = true;
        Debug.Log("Starting playback of " + frames.Count + " frames");

        if (frames.Count > 0)
        {
            foreach (RenderTexture frame in frames)
            {
                DisplayFrame(frame);
                yield return new WaitForSeconds(delayBetweenFrames);
            }
        }

        // Cleanup after playback
        foreach (RenderTexture frame in frames)
        {
            if (frame != null)
            {
                Destroy(frame);
            }
        }
        frames.Clear();
        
        replayRawImage.enabled = false;
        replayRawImage.texture = null;
        if (replayMaterial != null)
        {
            replayMaterial.mainTexture = null;
        }

        isPlaying = false;
        
        // Restore correct server text after replay
        if (player1._state == PlayerController.State.serverable)
        {
            player1.setServerText(true);
        }
        else if (player2._state == PlayerController.State.serverable)
        {
            if(!gameController.gameOver)
                player2.setServerText(true);
        }
    }

    private void DisplayFrame(RenderTexture frame)
    {
        if (frame != null)
        {
            replayRawImage.gameObject.SetActive(true);
            if (replayMaterial != null)
            {
                replayRawImage.texture = frame;
                replayMaterial.mainTexture = frame;
            }
            replayRawImage.color = Color.white; // Ensure full opacity and no color tint
        }
    }

    void OnDisable()
    {
        // Cleanup frame list
        foreach (RenderTexture frame in frames) if (frame != null) Destroy(frame);
        frames.Clear();

        if (replayRenderTexture != null)
        {
            replayRenderTexture.Release();
        }
    }
}
