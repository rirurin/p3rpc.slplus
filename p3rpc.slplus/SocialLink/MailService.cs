using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.SocialLink
{
    public class MailService : ModuleBase<SocialLinkContext>
    {
        private CommonHooks _common;
        private SocialLinkUtilities _utils;

        public enum EMailCategory
        {
            Community = 0,
            DeepEpisode = 1,
            Facility = 2,
            Common = 3,
            Max = 4,
        };

        public enum EMailCondition
        {
            Equal = 0,
            NotEqual = 1,
            Greater = 2,
            EGreater = 3,
            Less = 4,
            ELess = 5,
            EMailCondition_MAX = 6,
        };

        public enum EMailTime
        {
            Morning = 0,
            Noon = 1,
            Night = 2,
            EMailTime_MAX = 3,
        };


        [StructLayout(LayoutKind.Explicit, Size = 0x80)]
        public unsafe struct FMailIncomingItem
        {
            [FieldOffset(0x0000)] public ushort ID;
            [FieldOffset(0x0002)] public ushort SenderID;
            [FieldOffset(0x0004)] public ushort Group;
            [FieldOffset(0x0006)] public EMailCategory Category;
            [FieldOffset(0x0007)] public byte StartMonth;
            [FieldOffset(0x0008)] public byte StartDays;
            [FieldOffset(0x0009)] public byte EndMonth;
            [FieldOffset(0x000A)] public byte EndDays;
            [FieldOffset(0x000B)] public EMailTime ReceiveTime;
            [FieldOffset(0x000C)] public byte WeekFlag;
            [FieldOffset(0x000D)] public byte bWeekday;
            [FieldOffset(0x000D)] public byte bHoliday;
            [FieldOffset(0x000D)] public byte bRankUp;
            [FieldOffset(0x000D)] public byte bOnlyOnce;
            [FieldOffset(0x000E)] public byte ArcanaID;
            [FieldOffset(0x000F)] public EMailCondition ArcanaCondition;
            [FieldOffset(0x0010)] public byte Rank;
            [FieldOffset(0x0011)] public byte InviteCounter;
            [FieldOffset(0x0014)] public int CounterID;
            [FieldOffset(0x0018)] public EMailCondition CounterCondition;
            [FieldOffset(0x001C)] public int CounterValue;
            [FieldOffset(0x0020)] public FString MailBmdFileName;
            [FieldOffset(0x0030)] public int SenderLabelID;
            [FieldOffset(0x0034)] public int TitleLabelID;
            [FieldOffset(0x0038)] public int BodyLabelID;
            [FieldOffset(0x0040)] public FString ScriptBfFileName;
            [FieldOffset(0x0050)] public FString ScriptBmdFileName;
            [FieldOffset(0x0060)] public TArray<int> EnableFlags;
            [FieldOffset(0x0070)] public TArray<int> DisableFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x80)]
        public unsafe struct UMailIncomingDataAsset
        {
            [FieldOffset(0x0000)] public UAppMultiDataAsset baseObj;
            [FieldOffset(0x0030)] public TMap<int, FMailIncomingItem> Data;
        }

        private string UGlobalWork_GetMailIncomingDataAsset_SIG = "48 83 EC 28 E8 ?? ?? ?? ?? 48 85 C0 75 ?? 48 83 C4 28 C3 B2 10";
        public IHook<UGlobalWork_GetMailIncomingDataAsset> _getMailIncoming;
        public unsafe delegate UMailIncomingDataAsset* UGlobalWork_GetMailIncomingDataAsset();

        private string AMailActor_AddnewFlowAssetForMail_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B D9 48 8B EA";
        public IHook<AMailActor_AddNewFlowAssetForMail> _addNewFlowAssetForMail;
        public unsafe delegate nint AMailActor_AddNewFlowAssetForMail(nint a1, FString* fstr);

        public unsafe MailService(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UGlobalWork_GetMailIncomingDataAsset_SIG, "UGlobalWork::GetMailIncomingDataAsset", _context._utils.GetDirectAddress,
                addr => _getMailIncoming = _context._utils.MakeHooker<UGlobalWork_GetMailIncomingDataAsset>(UGlobalWork_GetMailIncomingDataAssetImpl, addr));
            _context._utils.SigScan(AMailActor_AddnewFlowAssetForMail_SIG, "AMailActor::AddnewFlowAssetForMail", _context._utils.GetDirectAddress,
                addr => _addNewFlowAssetForMail = _context._utils.MakeHooker<AMailActor_AddNewFlowAssetForMail>(AMailActor_AddNewFlowAssetForMailImpl, addr));
        }

        public override void Register()
        {
            //_manager = GetModule<SocialLinkManager>();
            _utils = GetModule<SocialLinkUtilities>();
            _common = GetModule<CommonHooks>();
        }

        public unsafe UMailIncomingDataAsset* UGlobalWork_GetMailIncomingDataAssetImpl()
        {
            var mailIncoming = (UMailIncomingDataAsset*)_context._objectMethods.GetSubsystem<UUIResources>((UGameInstance*)_common._getUGlobalWork())->GetAssetEntry(0x10);
            var mailData = &mailIncoming->Data;
            var mailSave = &_common._getUGlobalWork()->Mail;
            var newMailId = (ushort)(mailData->elements[mailData->mapNum - 1].Key + 1);
            var newMailIdHash = newMailId & (*(int*)((nint)mailData + 0x48) - 1);
            _context._utils.Log($"new mail ID: 0x{newMailId:X} (hash 0x{newMailIdHash:X})");
            /*
            mailSave->MailCount = 1;
            //_context._utils.Log($"{mailSave->MailCount} mail entries (max key: {})");
            */
            for (uint i = 0; i < mailSave->MailCount; i++)
            {
                //_context._utils.Log($"{mailSave->GetMailEntry(i)->MailIncomingKey}");
                mailSave->GetMailEntry(i)->MailIncomingKey = newMailId;
            }
            _context._utils.Log($"[UGlobalWork::GetMailIncomingDataAssetImpl] {(nint)(&mailIncoming->Data):X} num: {mailIncoming->Data.mapNum} ({(mailIncoming->Data.mapNum + 1):X}), max: {mailIncoming->Data.mapMax}");
            // test adding a new mail asset
            FMailIncomingItem newMailItem = new FMailIncomingItem();
            newMailItem.ID = newMailId;
            newMailItem.SenderID = 36; // sprite id in SPR_UI_Mail_Main (T_UI_Mail_Main_00_texture)
            newMailItem.Group = 1;
            newMailItem.Category = EMailCategory.Community;
            newMailItem.StartMonth = 4;
            newMailItem.StartDays = 1;
            newMailItem.EndMonth = 0;
            newMailItem.EndDays = 0;
            newMailItem.ReceiveTime = EMailTime.Noon;
            newMailItem.WeekFlag = 52;
            newMailItem.bWeekday = 1;
            newMailItem.bHoliday = 0;
            newMailItem.bRankUp = 1;
            newMailItem.bOnlyOnce = 1;
            newMailItem.ArcanaID = 2;
            newMailItem.ArcanaCondition = EMailCondition.Equal;
            newMailItem.Rank = 1;
            newMailItem.InviteCounter = 0;
            newMailItem.CounterID = 0;
            newMailItem.CounterCondition = EMailCondition.Equal;
            newMailItem.CounterValue = 0;
            newMailItem.MailBmdFileName = _utils.MakeFString(Constants.MakeAssetPath($"{Constants.MailTextBmds}BMD_MailText_Saori"));
            newMailItem.SenderLabelID = 0;
            newMailItem.TitleLabelID = 1;
            newMailItem.BodyLabelID = 2;
            newMailItem.ScriptBfFileName = _utils.MakeFString("/Game/Xrd777/UI/Mail/BF_Mailscr_Cmmu_Tomochika_Rankup.BF_Mailscr_Cmmu_Tomochika_Rankup");
            newMailItem.ScriptBmdFileName = _utils.MakeFString("/Game/Xrd777/UI/Mail/BMD_Mailscr_Cmmu_Tomochika_Rankup.BMD_Mailscr_Cmmu_Tomochika_Rankup");
            newMailItem.EnableFlags = new TArray<int>();
            _context._memoryMethods.TArray_Insert(&newMailItem.EnableFlags, 0);
            _context._memoryMethods.TArray_Insert(&newMailItem.EnableFlags, 0);
            _context._memoryMethods.TArray_Insert(&newMailItem.EnableFlags, 0);
            newMailItem.DisableFlags = new TArray<int>();
            _context._memoryMethods.TArray_Insert(&newMailItem.DisableFlags, 268435649);
            _context._memoryMethods.TArray_Insert(&newMailItem.DisableFlags, 268435489);
            _context._memoryMethods.TArray_Insert(&newMailItem.DisableFlags, 0);
            var bMapInsertSuccess = _context._memoryMethods.TMap_InsertNoInit((TMap<HashableInt, FMailIncomingItem>*)&mailIncoming->Data, new HashableInt(newMailId), newMailItem);
            //_context._utils.Log($"{(nint)(&_common._getUGlobalWork()->Mail):X}");
            _context._utils.Log($"{(nint)(&mailIncoming->Data):X}, {bMapInsertSuccess}");
            return mailIncoming;
        }

        public unsafe nint AMailActor_AddNewFlowAssetForMailImpl(nint a1, FString* fstr)
        {
            //_context._utils.Log($"[AMailActor::AddNewFlowAssetForMail] {*fstr}");
            _context._utils.Log($"[AMailActor::AddNewFlowAssetForMail] {(nint)fstr:X}");
            return _addNewFlowAssetForMail.OriginalFunction(a1, fstr);
        }
    }
}
