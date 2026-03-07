#nullable disable
using Il2Cpp;
using Il2CppMV.Common;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace KogamaStudio.Players;

internal class PlayerList
{
    internal static void Start()
    {
        MelonCoroutines.Start(UpdateLoop());
    }

    private static IEnumerator UpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            SendPlayersUpdate();
        }
    }

    internal static void SendPlayersUpdate()
    {
        var game = MVGameControllerBase.Game;
        if (game == null || game.playerContainer == null) return;

        var players = game.playerContainer.players;
        var entries = new System.Collections.Generic.List<string>();
        foreach (var player in players)
            entries.Add($"{player.Value.WoId}:{player.Value.UserProfileData.UserName}");

        PipeClient.SendCommand("players_list|" + string.Join(",", entries));
    }
}
