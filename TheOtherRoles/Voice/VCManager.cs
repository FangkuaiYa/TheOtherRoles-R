using TheOtherRoles.Voice.Game;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TheOtherRoles.Voice;

internal class VCManager : MonoBehaviour
{
    static VCManager()
    {
        ClassInjector.RegisterTypeInIl2Cpp<VCManager>();
    }

    private void Update()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "OnlineGame":
            case "EndGame":
                TorVoiceHudState.UpdateHud();
                TorVoiceRoomDriver.Update();
                break;
        }
    }

    internal static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded +=
            (UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        new GameObject("VC_Manager").AddComponent<VCManager>();

        switch (scene.name)
        {
            case "MainMenu":
            case "MatchMaking":
                VoiceRoom.CloseCurrentRoom();
                VoiceJoinPrompt.Reset();
                break;
        }
    }
}