using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features.Waves;
using Respawning.Waves;
using PlayerEvents = Exiled.Events.Handlers.Player;

namespace SpotLastSurvivor
{
    public class Plugin : Plugin<Config>
    {
        public override string Name { get; } = "SpotLastSurvivor";
        public override string Author { get; } = "Antoniofo";

        public override Version Version => new Version(2, 0, 1);
        public override Version RequiredExiledVersion { get; } = new Version(9, 6, 0);

        public override string Prefix => "sls";

        public static Plugin Instance;

        private TimedWave _ntfWave;
        private TimedWave _chaosWave;
        private TimedWave _ntfMiniWave;
        private TimedWave _chaosMiniWave;

        public static bool Active = true;

        private TimeSpan NtfRespawnTime()
        {
            if (_ntfMiniWave != null && !_ntfMiniWave.Timer.IsPaused)
            {
                return _ntfMiniWave.Timer.TimeLeft;
            }

            return _ntfWave != null ? _ntfWave.Timer.TimeLeft : TimeSpan.Zero;
        }

        private TimeSpan ChaosRespawnTime()
        {
            if (_chaosMiniWave != null && !_chaosMiniWave.Timer.IsPaused)
            {
                return _chaosMiniWave.Timer.TimeLeft;
            }

            return _chaosWave != null ? _chaosWave.Timer.TimeLeft : TimeSpan.Zero;
        }

        public override void OnEnabled()
        {
            Instance = this;
            PlayerEvents.Died += OnPlayerDied;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStart;
        }

        public override void OnDisabled()
        {
            PlayerEvents.Died -= OnPlayerDied;
            Instance = null;
            Timing.KillCoroutines();
        }

        public void OnRoundStart()
        {
            if (TimedWave.TryGetTimedWave<NtfSpawnWave>(out TimedWave ntfWave))
            {
                Log.Debug("NtfWave found");
                _ntfWave = ntfWave;
            }

            if (TimedWave.TryGetTimedWave<NtfMiniWave>(out TimedWave ntfMiniWave))
            {
                Log.Debug("NtfMiniWave found");
                _ntfMiniWave = ntfMiniWave;
            }

            if (TimedWave.TryGetTimedWave<ChaosSpawnWave>(out TimedWave chaosWave))
            {
                Log.Debug("ChaosWave found");
                _chaosWave = chaosWave;
            }

            if (TimedWave.TryGetTimedWave<ChaosMiniWave>(out TimedWave chaosMiniWave))
            {
                Log.Debug("ChaosMiniWave found");
                _chaosMiniWave = chaosMiniWave;
            }
        }

        public void OnPlayerDied(DiedEventArgs ev)
        {
            List<Player> list = Player.List.Where(x => !x.IsDead && !x.IsScp && !x.IsTutorial).ToList();
            if (list.Count != 1)
                return;
            TimeSpan ntfTime = NtfRespawnTime() + TimeSpan.FromSeconds(18);
            if (ntfTime < TimeSpan.Zero) ntfTime = TimeSpan.Zero;
            TimeSpan chaosTime = ChaosRespawnTime() + TimeSpan.FromSeconds(13);
            if (chaosTime < TimeSpan.Zero) chaosTime = TimeSpan.Zero;

            Log.Debug("NTF TotalSeconds: " + ntfTime.TotalSeconds);
            Log.Debug("Chaos TotalSeconds: " + chaosTime.TotalSeconds);
            if (ntfTime.TotalSeconds < Config.RespawnTiming || chaosTime.TotalSeconds < Config.RespawnTiming)
                return;
            if (!Plugin.Active)
                return;

            if (Config.CassieScanAnnounce)
            {
                Log.Debug("Cassie SCAN Message");
                Cassie.Message(Config.CassieAnnounceMessage, true, true, true);
            }

            Timing.CallDelayed(Config.ScanningTime, () =>
            {
                Log.Debug("Doxxing dudes");
                list = Player.List.Where(x => !x.IsDead && !x.IsScp && !x.IsTutorial).ToList();
                if (list.Count == 1)
                {
                    Player ply = list.FirstOrDefault();
                    Log.Debug("Role: " + ply.Role.Name);
                    Log.Debug("Team: " + ply.Role.Team);
                    string roleName = ply.Role.Name;
                    if (ply.Role.Team == Team.FoundationForces && ply.Role != RoleTypeId.FacilityGuard)
                        roleName = "M T F";
                    if (ply.Role.Team == Team.ChaosInsurgency)
                        roleName = "ChaosInsurgency";
                    Log.Debug("SPOTTED AT " + ply.CurrentRoom.Name + " as " + roleName.Replace("-", ""));
                    Cassie.Message("SPOTTED AT " + ply.CurrentRoom.Name + " as " + roleName.Replace("-", ""), true,
                        true, true);
                }
            });
        }
    }
}