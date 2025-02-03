using System;
using UnityEngine;
using WiimoteApi;

public class WiimoteController : MonoBehaviour
{
    public static WiimoteController Instance { get; private set; }
    private static bool wiimotesInitialized = false;
    private Wiimote[] wiimotes = new Wiimote[2];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public Wiimote GetWiimote(int index)
    {
        if (index < 0 || index >= wiimotes.Length)
            return null;
        return wiimotes[index];
    }

    public void InitializeWiimotes()
    {
        if (!wiimotesInitialized)
        {
            try{
                WiimoteManager.FindWiimotes();
            }catch(Exception e){
                Debug.LogError(e);
            }
            Debug.Log("number :" + WiimoteManager.Wiimotes.Count.ToString());
            if (WiimoteManager.Wiimotes.Count >= 2)
            {
                // Set up first Wiimote (Player 1)
                wiimotes[0] = WiimoteManager.Wiimotes[0];
                wiimotes[0].SendStatusInfoRequest();
                wiimotes[0].SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL);
                wiimotes[0].SendPlayerLED(true, false, false, false); // LED 1

                // Set up second Wiimote (Player 2)
                wiimotes[1] = WiimoteManager.Wiimotes[1];
                wiimotes[1].SendStatusInfoRequest();
                wiimotes[1].SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL);
                wiimotes[1].SendPlayerLED(false, true, false, false); // LED 2

                wiimotesInitialized = true;
                Debug.Log("Both Wiimotes initialized successfully!");
            }
            else if(WiimoteManager.Wiimotes.Count == 1){
                 // Set up first Wiimote (Player 1)
                wiimotes[0] = WiimoteManager.Wiimotes[0];
                wiimotes[0].SendStatusInfoRequest();
                wiimotes[0].SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL);
                wiimotes[0].SendPlayerLED(true, false, false, false); // LED 1
            }
            else
            {
                Debug.LogError($"Not enough Wiimotes found. Found: {WiimoteManager.Wiimotes.Count}, Need: 2");
            }
        }
    }

    private void OnApplicationQuit()
    {
        foreach (Wiimote wiimote in wiimotes)
        {
            if (wiimote != null)
            {
                WiimoteManager.Cleanup(wiimote);
            }
        }
    }
}
