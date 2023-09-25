using Footprinting;

using helpers;

using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.BasicMessages;

using PlayerRoles;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Compendium.Custom.Items.Firearms
{
    public class CustomFirearmItemHandler : CustomItemHandler<Firearm>
    {
        internal DateTime _lastShot;
        internal bool _everShot;

        internal List<ReferenceHub> _playerTargetHistory = new List<ReferenceHub>();
        internal List<GameObject> _targetHistory = new List<GameObject>();

        public byte Ammo
        {
            get => Status.Ammo;
            set => Status = new FirearmStatus(value, StatusFlags, AttachmentsCode);
        }

        public byte MaxAmmo
        {
            get => Item?.AmmoManagerModule?.MaxAmmo ?? 0;
            set => CustomItemOverrides.SetMaxAmmoOverride(Item, value, false);
        }

        public uint AttachmentsCode
        {
            get => Status.Attachments;
            set => Status = new FirearmStatus(Ammo, StatusFlags, value);
        }

        public int MillisecondsSinceLastShot
        {
            get => WasEverShot ? -1 : (DateTime.Now - _lastShot).Milliseconds;
        }

        public float ArmorPenetration
        {
            get => Item?.ArmorPenetration ?? 0f;
            set => CustomItemOverrides.SetPenetrationOverride(Item, value, value < 0f);
        }

        public float Length
        {
            get => Item?.Length ?? 0f;
            set => CustomItemOverrides.SetLengthOverride(Item, value, value < 0f);
        }

        public float BaseLength
        {
            get => Item?.BaseLength ?? 0f;
            set => Item!.BaseLength = value;
        }

        public bool WasEverShot
        {
            get => _everShot;
        }

        public bool IsEmittingLight
        {
            get => Item?.IsEmittingLight ?? false;
        }

        public bool IsAiming
        {
            get => Item?.AdsModule?.ServerAds ?? false;
            set => Item!.AdsModule!.ServerAds = value;
        }

        public bool IsFootprintValid
        {
            get => Item?._footprintValid ?? false;
            set => Item!._footprintValid = value;
        }

        public bool IsDisarming
        {
            get => Item?.AllowDisarming ?? false;
            set => CustomItemOverrides.SetDisarmOverride(Item, value, false);
        }

        public bool IsFlashlightEnabled
        {
            get => StatusFlags.HasFlagFast(FirearmStatusFlags.FlashlightEnabled);
            set => StatusFlags = Item?.OverrideFlashlightFlags(value) ?? StatusFlags;
        }

        public bool IsChambered
        {
            get => StatusFlags.HasFlagFast(FirearmStatusFlags.Chambered);
            set => StatusFlags = value ? (StatusFlags | FirearmStatusFlags.Chambered) : (StatusFlags & ~FirearmStatusFlags.Chambered);
        }

        public bool IsCocked
        {
            get => StatusFlags.HasFlagFast(FirearmStatusFlags.Cocked);
            set => StatusFlags = value ? (StatusFlags | FirearmStatusFlags.Cocked) : (StatusFlags & ~FirearmStatusFlags.Cocked);
        }

        public ItemType AmmoType
        {
            get => Item?.AmmoType ?? ItemType.None;
        }

        public Faction Affiliation
        {
            get => Item?.FirearmAffiliation ?? Faction.Unclassified;
        }

        public FirearmStatusFlags StatusFlags
        {
            get => Status.Flags;
            set => Status = new FirearmStatus(Ammo, value, AttachmentsCode);
        }

        public AttachmentName[] EnabledAttachments
        {
            get => FirearmHelper.GetAttachments(Type, AttachmentsCode);
            set => AttachmentsCode = FirearmHelper.SetEnabledAttachmentsCode(Type, value);
        }

        public FirearmStatus Status
        {
            get => Item?.Status ?? default;
            set => Item!.Status = value;
        }

        public FirearmStatus PredictedStatus
        {
            get => Item?.ActionModule?.PredictedStatus ?? default;
        }

        public FirearmBaseStats Stats
        {
            get => Item?.BaseStats ?? default;
        }

        public Footprint Footprint
        {
            get => Item?.Footprint ?? default;
            set
            {
                if (Item is null)
                    return;

                Item._lastFootprint = value;
                Item._footprintValid = true;
            }
        }

        public DateTime LastShot
        {
            get => _lastShot;
        }

        public IReadOnlyCollection<ReferenceHub> PlayerTargetHistory
        {
            get => _playerTargetHistory;
        }

        public IReadOnlyCollection<GameObject> TargetHistory
        {
            get => _targetHistory;
        }

        public bool Reload()
            => Item != null && Item.AmmoManagerModule.ServerTryReload();

        public bool StopReload()
            => Item != null && Item.AmmoManagerModule.ServerTryStopReload();

        public bool Unload()
            => Item != null && Item.AmmoManagerModule.ServerTryUnload();

        public bool HasEverShot(ReferenceHub hub)
            => _playerTargetHistory.Contains(hub);

        public void EnableAttachment(AttachmentName attachmentName)
        {
            var cur = Pools.PoolList(EnabledAttachments);

            if (!cur.Contains(attachmentName))
                cur.Add(attachmentName);

            EnabledAttachments = cur.ToArray();
            cur.ReturnList();
        }

        public void DisableAttachment(AttachmentName attachmentName)
        {
            var cur = Pools.PoolList(EnabledAttachments);

            if (cur.Contains(attachmentName))
                cur.Remove(attachmentName);

            EnabledAttachments = cur.ToArray();
            cur.ReturnList();
        }

        public void ModifyStatus(Action<FirearmStatus> action)
        {
            var status = Status;
            action?.Invoke(status);
            Status = status;
        }

        public virtual bool OnShootingPlayer(ReferenceHub target, ReferenceHub shooter, Vector3 position, float distance, ref float damage) => true;

        public virtual void OnShotPlayer(ReferenceHub target, ReferenceHub shooter, Vector3 position, float distance, float damage) 
        {
            _playerTargetHistory.Add(target);
        }

        public virtual bool OnShootingOther(GameObject target, ReferenceHub shooter, Vector3 position, float distance) => true;

        public virtual void OnShotTarget(GameObject target, ReferenceHub shooter, Vector3 position, float distance) 
        {
            _targetHistory.Add(target);
        }

        public virtual bool OnReloading() => true;
        public virtual void OnReload() { }

        public virtual bool OnAiming() => true;
        public virtual void OnAimed() { }

        public virtual bool OnAimingStop() => true;
        public virtual void OnAimingStopped() { }

        public virtual bool OnEnablingFlashlight() => true;
        public virtual void OnEnabledFlashlight() { }

        public virtual bool OnDisablingFlashlight() => true;
        public virtual void OnDisabledFlashlight() { }

        public virtual bool OnStoppingReload() => true;
        public virtual void OnStoppedReload() { }

        public virtual bool OnInspecting() => true;
        public virtual void OnInspected() { }

        public virtual void OnDryShot() { }

        public virtual bool OnUnloading() => true;
        public virtual void OnUnload() { }

        public override void OnItemDestroyed()
        {
            base.OnItemDestroyed();

            _everShot = false;
            _lastShot = default;
            _playerTargetHistory.Clear();
            _targetHistory.Clear();
        }
    }
}