using helpers;

using InventorySystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;

using System;

namespace Compendium.Custom.Items.Firearms
{
    public static class FirearmHelper
    {
        public static uint SetEnabledAttachmentsCode(ItemType firearmType, AttachmentName[] attachments)
        {
            if (!InventoryItemLoader.TryGetItem<Firearm>(firearmType, out var firearm)
                || firearm.Attachments is null)
                return 0;

            var curCode = firearm.GetCurrentAttachmentsCode();

            for (int i = 0; i < firearm.Attachments.Length; i++)
            {
                if (!attachments.Contains(firearm.Attachments[i].Name))
                    firearm.Attachments[i].IsEnabled = false;
                else
                    firearm.Attachments[i].IsEnabled = true;
            }

            var code = firearm.GetCurrentAttachmentsCode();

            firearm.ApplyAttachmentsCode(curCode, false);

            return code;
        }

        public static AttachmentName[] GetAttachments(ItemType firearmType, uint code)
        {
            if (!InventoryItemLoader.TryGetItem<Firearm>(firearmType, out var firearm)
                || firearm.Attachments is null)
                return Array.Empty<AttachmentName>();

            var curCode = firearm.GetCurrentAttachmentsCode();

            firearm.ApplyAttachmentsCode(code, true);

            var attachments = Pools.PoolList<AttachmentName>();

            firearm.Attachments.ForEach(attach =>
            {
                if (attach.IsEnabled)
                    attachments.Add(attach.Name);
            });

            firearm.ApplyAttachmentsCode(curCode, false);

            var array = attachments.ToArray();

            attachments.ReturnList();

            return array;
        }
    }
}
