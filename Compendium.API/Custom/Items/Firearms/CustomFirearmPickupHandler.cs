using helpers;

using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.BasicMessages;

using System;

namespace Compendium.Custom.Items.Firearms
{
    public class CustomFirearmPickupHandler : CustomPickupHandler<FirearmPickup>
    {
        public bool IsDistributed
        {
            get => Item?.Distributed ?? false;
            set => Item!.Distributed = value;
        }

        public bool IsFlashlightEnabled
        {
            get => StatusFlags.HasFlagFast(FirearmStatusFlags.FlashlightEnabled);
            set => StatusFlags = value ? (StatusFlags | FirearmStatusFlags.FlashlightEnabled) : (StatusFlags & ~FirearmStatusFlags.FlashlightEnabled);
        }

        public byte Ammo
        {
            get => Status.Ammo;
            set => Status = new FirearmStatus(value, StatusFlags, AttachmentCodes);
        }

        public uint AttachmentCodes
        {
            get => Status.Attachments;
            set => Status = new FirearmStatus(Ammo, StatusFlags, value);
        }

        public FirearmStatusFlags StatusFlags
        {
            get => Status.Flags;
            set => Status = new FirearmStatus(Ammo, value, AttachmentCodes);
        }

        public AttachmentName[] EnabledAttachments
        {
            get => FirearmHelper.GetAttachments(Type, AttachmentCodes);
            set => AttachmentCodes = FirearmHelper.SetEnabledAttachmentsCode(Type, value);
        }

        public FirearmStatus Status
        {
            get => Item?.Status ?? default;
            set => Item!.Status = value;
        }

        public void ModifyStatus(Action<FirearmStatus> action)
        {
            var status = Status;
            action?.Invoke(status);
            Status = status;
        }

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
    }
}