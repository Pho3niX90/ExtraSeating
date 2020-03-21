using Oxide.Core;
using System;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Extra Seating", "Pho3niX90", "1.0.4")]
    [Description("Allows extra seats on minicopters and horses")]
    class ExtraSeating : RustPlugin {
        #region Config
        public PluginConfig config;
        static ExtraSeating _instance;
        bool debug = false;
        int seats = 0;

        bool onMiniCanCreateRotorSeat;
        bool onMiniCanCreateSideSeats;
        bool onHorseCanCreateBackSeat;

        string CHAIR_PREFAB = "assets/prefabs/vehicle/seats/passengerchair.prefab";
        string INVIS_CHAIR_PREFAB = "assets/bundled/prefabs/static/chair.invisible.static.prefab";

        protected override void LoadDefaultConfig() { Config.WriteObject(GetDefaultConfig(), true); }
        public PluginConfig GetDefaultConfig() { return new PluginConfig { EnableMiniSideSeats = false, EnableMiniBackSeat = false, EnableExtraHorseSeat = false }; }
        public class PluginConfig { public bool EnableMiniSideSeats; public bool EnableMiniBackSeat; public bool EnableExtraHorseSeat; }
        #endregion
        private void Init() {
            config = Config.ReadObject<PluginConfig>();
        }

        void LogDebug(string str) {
            if (debug) Puts(str);
        }

        void OnEntitySpawned(BaseNetworkable entity) {
            _instance = this;
            if (entity == null || !(entity is MiniCopter || entity is RidableHorse)) return;
            BaseVehicle vehicle = entity as BaseVehicle;
            BaseEntity be = entity as BaseEntity;

            seats = vehicle.mountPoints.Length; // default

            if (entity is MiniCopter && entity.ShortPrefabName.Equals("minicopter.entity")) {
                var rotor = Interface.CallHook("OnMiniCanCreateRotorSeat", entity);
                var sides = Interface.CallHook("OnMiniCanCreateSideSeats", entity);
                onMiniCanCreateRotorSeat = rotor != null ? (bool)rotor : false;
                onMiniCanCreateSideSeats = sides != null ? (bool)sides : false;

                if (_instance.config.EnableMiniSideSeats || onMiniCanCreateSideSeats) seats += 2;
                if (_instance.config.EnableMiniBackSeat || onMiniCanCreateRotorSeat) seats += 1;

                if (vehicle.mountPoints.Length < seats)
                    vehicle?.gameObject.AddComponent<Seating>();
            }

            if (entity is RidableHorse) {
                var horse = Interface.CallHook("OnHorseCanCreateBackSeat", entity);
                onHorseCanCreateBackSeat = horse != null ? (bool)horse : false;

                if (_instance.config.EnableExtraHorseSeat || onHorseCanCreateBackSeat) seats += 1;

                if (vehicle.mountPoints.Length < seats)
                    NextTick(() => {
                        vehicle?.gameObject.AddComponent<Seating>();
                    });
            }
        }

        void AddSeat(BaseVehicle ent, Vector3 locPos, Quaternion q) {
            string PREFAB = ent is MiniCopter ? CHAIR_PREFAB : INVIS_CHAIR_PREFAB;
            BaseEntity seat = GameManager.server.CreateEntity(PREFAB, ent.transform.position, q) as BaseEntity;
            if (seat == null) return;

            seat.SetParent(ent);
            seat.Spawn();
            seat.transform.localPosition = locPos;
            seat.SendNetworkUpdateImmediate(true);
        }

        BaseVehicle.MountPointInfo CreateMount(Vector3 vec, BaseVehicle.MountPointInfo exampleSeat) {
            return CreateMount(vec, exampleSeat, new Vector3());
        }

        BaseVehicle.MountPointInfo CreateMount(Vector3 vec, BaseVehicle.MountPointInfo exampleSeat, Vector3 rotation) {
            return new BaseVehicle.MountPointInfo {
                pos = vec,
                rot = rotation != null ? rotation : new Vector3(0, 0, 0),
                bone = exampleSeat.bone,
                prefab = exampleSeat.prefab,
                mountable = exampleSeat.mountable
            };
        }

        #region Classes
        class Seating : FacepunchBehaviour {
            public BaseVehicle entity;
            void Awake() {
                entity = GetComponent<BaseVehicle>();
                if (entity == null) { Destroy(this); return; }

                bool isMini = entity is MiniCopter;
                bool isHorse = entity is RidableHorse;
                Vector3 emptyVector = new Vector3(0, 0, 0);

                if (isMini) _instance.LogDebug("Minicopter detected");
                if (isHorse) _instance.LogDebug("Horse detected");

                BaseVehicle.MountPointInfo pilot = entity.mountPoints[0];

                if (isHorse) {
                    _instance.LogDebug("Adding passenger seat");
                    //Vector3 horseVector = new Vector3(0f, -0.32f, -0.5f);
                    if (_instance.config.EnableExtraHorseSeat || _instance.onHorseCanCreateBackSeat) {
                        Vector3 horseVector2 = new Vector3(0f, 1.0f, -0.5f);
                        //BaseVehicle.MountPointInfo horseBack = _instance.CreateMount(horseVector, pilot);
                        //entity.mountPoints[0] = pilot;
                        //entity.mountPoints[1] = horseBack;
                        _instance.AddSeat(entity, horseVector2, new Quaternion());
                    }
                }

                if (isMini) {
                    Array.Resize(ref entity.mountPoints, _instance.seats);
                    BaseVehicle.MountPointInfo pFront = entity.mountPoints[1];
                    Vector3 leftVector = new Vector3(0.6f, 0.2f, -0.2f);
                    Vector3 rightVector = new Vector3(-0.6f, 0.2f, -0.2f);
                    Vector3 backVector = new Vector3(0.0f, 0.4f, -1.2f);
                    Vector3 backVector2 = new Vector3(0.0f, 0.4f, -1.45f);

                    Vector3 playerOffsetVector = new Vector3(0f, 0f, -0.25f);
                    Quaternion backQuaternion = Quaternion.Euler(0f, 180f, 0f);

                    if (_instance.config.EnableMiniSideSeats || _instance.onMiniCanCreateSideSeats) {
                        _instance.LogDebug("Adding side seats");
                        BaseVehicle.MountPointInfo pLeftSide = _instance.CreateMount(leftVector, pFront);
                        BaseVehicle.MountPointInfo pRightSide = _instance.CreateMount(rightVector, pFront);
                        entity.mountPoints[2] = pLeftSide;
                        entity.mountPoints[3] = pRightSide;
                        _instance.AddSeat(entity, leftVector + playerOffsetVector, new Quaternion());
                        _instance.AddSeat(entity, rightVector + playerOffsetVector, new Quaternion());
                    }

                    if (_instance.config.EnableMiniBackSeat || _instance.onMiniCanCreateRotorSeat) {
                        _instance.LogDebug("Adding back/rotor seat");
                        BaseVehicle.MountPointInfo pBackReverse = _instance.CreateMount(backVector2, pFront, new Vector3(0f, 180f, 0f));
                        entity.mountPoints[_instance.seats - 1] = pBackReverse;
                        _instance.AddSeat(entity, backVector, backQuaternion);
                    }
                }

            }

        }
        #endregion
    }
}
