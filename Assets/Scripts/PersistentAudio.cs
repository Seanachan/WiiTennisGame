using UnityEngine;

public class PersistentAudio : MonoBehaviour
{
    private static PersistentAudio instance;

    public string audioSourceTag; // 給每個 Audio Source 設置唯一的標籤

    void Awake()
    {
        // 確保只有一個帶有該標籤的 Audio Source
        var existingAudioSource = GameObject.FindWithTag(audioSourceTag);
        if (existingAudioSource != null && existingAudioSource != gameObject)
        {
            Destroy(gameObject); // 銷毀重複的 Audio Source
            return;
        }

        gameObject.tag = audioSourceTag; // 設置標籤
        DontDestroyOnLoad(gameObject); // 保留該物件
    }
}
