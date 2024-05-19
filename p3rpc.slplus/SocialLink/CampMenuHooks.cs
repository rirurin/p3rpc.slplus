using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.SocialLink
{
    public class CommuListColors
    {
        public FSprColor BgColorNormal { get; set; } = new FSprColor(0xe, 0xe, 0x54, 0xff);
        public FSprColor BgColorSelected { get; set; } = new FSprColor(0xff, 0xff, 0xff, 0xff);
        // public FSprColor BgColorReverse { get; set; } = new FSprColor(0x0, 0x0, 0x0, 0xff); (Not implemented in P3RE)
        public FSprColor FgColorNormal { get; set; } = new FSprColor(0x72, 0xff, 0xff, 0xff);
        public FSprColor FgColorReverse { get; set; } = new FSprColor(0xff, 0x0, 0x0, 0xff);
        public FSprColor FgColorSelected { get; set; } = new FSprColor(0x0, 0x0, 0x0, 0xff);
        public FSprColor CursorColor { get; set; } = new FSprColor(0xff, 0x0, 0x0, 0xff);
        public FSprColor ListTitleColor { get; set; } = new FSprColor(0xff, 0xff, 0xff, 0xff);

        /* TODO: Move this to native types */
        public unsafe FSprColor GetBackgroundColorFromMenu(CampMenuHooks.CmpCommuMenu* menu, int id)
            => (menu->VisibleEntryOffset == id) ? BgColorSelected : BgColorNormal;

        public unsafe FSprColor GetForegroundColorFromMenu(CampMenuHooks.CmpCommuMenu* menu, int id)
            => (menu->VisibleEntryOffset == id) ? FgColorSelected : FgColorNormal;
    }
    public class CampMenuHooks : ModuleBase<SocialLinkContext>
    {
        private string UCmpCommuList_DrawSocialLinkList_SIG = "E8 ?? ?? ?? ?? 4C 8B C5 BA 15 00 00 00";
        private IHook<UCmpCommuList_DrawSocialLinkList> _drawSocialLinkList;
        public unsafe delegate void UCmpCommuList_DrawSocialLinkList(UCmpCommuList* self, int drawId, USprAsset* campSpr);

        // NOTE: Femc Mod also uses this hook, this could be made as a shared scan submitter since it'll depend on this
        private string DrawSpriteDetailedParams_SIG = "48 8B C4 48 81 EC A8 00 00 00 F3 0F 10 84 24 ?? ?? ?? ?? F3 0F 10 8C 24 ?? ?? ?? ?? C6 40 ?? 00 C6 40 ?? 00 48 C7 40 ?? 00 00 00 00 C6 40 ?? 00 C6 40 ?? 00 C6 40 ?? 00";
        public DrawSpriteDetailedParams _drawSprDetailedParams;
        public unsafe delegate void DrawSpriteDetailedParams(USprAsset* spr, uint a2, uint id, float X, float Y, float Z, FSprColor color, int queueId, float a9, float a10, float a11, int a12, byte a13);

        private string UCmpCommuList_DrawQuad_SIG = "48 8B C4 48 89 58 ?? 55 48 8D 68 ?? 48 81 EC A0 00 00 00 0F 29 70 ??";
        public UCmpCommuList_DrawQuad _cmpDrawQuad;
        public unsafe delegate void UCmpCommuList_DrawQuad(FVector2D* points, float x, float y, float z, FSprColor color, int queueId, float a7, float a8);

        private string UCmpCommuList_DrawRoundRect_SIG = "48 8B C4 F3 0F 11 40 ?? 55 53 57";
        private UCmpCommuList_DrawRoundRect _drawRoundRect;
        public unsafe delegate void UCmpCommuList_DrawRoundRect(float x, float y, float z, float sx, float sy, int a6, FSprColor color, int drawId, float a9, float a10, float a11);

        private string AUIDrawBaseActor_FontDrawEx_SIG = "E8 ?? ?? ?? ?? 48 8B 4D ?? E9 ?? ?? ?? ?? 44 0F 29 9C 24 ?? ?? ?? ??";
        private AUIDrawBaseActor_FontDrawEx _fontDrawEx;
        public unsafe delegate void AUIDrawBaseActor_FontDrawEx(float x, float y, float z, FColor color, float size, float angle, float anglePointX, float anglePointY, FString* name, uint drawId, uint drawPoint, float* transformMtx);

        private unsafe FVector2D* cmmListEntryPoints;

        private CommuListColors listColors = new();
        public unsafe CampMenuHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UCmpCommuList_DrawSocialLinkList_SIG, "UCmpCommuList::DrawSocialLinkList", _context._utils.GetIndirectAddressShort,
                addr => _drawSocialLinkList = _context._utils.MakeHooker<UCmpCommuList_DrawSocialLinkList>(UCmpCommuList_DrawSocialLinkListImpl, addr));

            _context._utils.SigScan(DrawSpriteDetailedParams_SIG, "DrawSpriteDetailedParams", _context._utils.GetDirectAddress, addr => _drawSprDetailedParams = _context._utils.MakeWrapper<DrawSpriteDetailedParams>(addr));
            _context._utils.SigScan(UCmpCommuList_DrawQuad_SIG, "UCmpCommuList::DrawQuad", _context._utils.GetDirectAddress, addr => _cmpDrawQuad = _context._utils.MakeWrapper<UCmpCommuList_DrawQuad>(addr));
            _context._utils.SigScan(UCmpCommuList_DrawRoundRect_SIG, "UCmpCommuList::DrawRoundRect", _context._utils.GetDirectAddress, addr => _drawRoundRect = _context._utils.MakeWrapper<UCmpCommuList_DrawRoundRect>(addr));
            _context._utils.SigScan(AUIDrawBaseActor_FontDrawEx_SIG, "AUIDrawBaseActor::FontDrawEx", _context._utils.GetIndirectAddressShort, addr => _fontDrawEx = _context._utils.MakeWrapper<AUIDrawBaseActor_FontDrawEx>(addr));

            cmmListEntryPoints = (FVector2D*)NativeMemory.Alloc((nuint)sizeof(FVector2D) * 4);
            cmmListEntryPoints[0].X = -303.0f;
            cmmListEntryPoints[0].Y = -68.0f;
            cmmListEntryPoints[1].X = 280.0f;
            cmmListEntryPoints[1].Y = -68.0f;
            cmmListEntryPoints[2].X = -280.0f;
            cmmListEntryPoints[2].Y = 69.0f;
            cmmListEntryPoints[3].X = 303.0f;
            cmmListEntryPoints[3].Y = 69.0f;
        }
        public override void Register()
        {
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xa0)]
        public unsafe struct UCmpCommuListVisibleEntries
        {
            [FieldOffset(0x0)] public uint Flags;
            [FieldOffset(0x28)] public float Opacity;
            [FieldOffset(0x34)] public float Field34;
            [FieldOffset(0x38)] public float Field38;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x50)]
        public unsafe struct CmpCommuMenu
        {
            [FieldOffset(0x4)] public int EntryCount;
            [FieldOffset(0x8)] public int EntriesVisibleAtOnce;
            [FieldOffset(0x24)] public int VisibleEntryOffset;
            [FieldOffset(0x28)] public int ScrollEntryOffset;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x478)]
        public unsafe struct AUICmpCommu
        {
            [FieldOffset(0x0000)] public AUIBaseActor baseObj;
            //[FieldOffset(0x0408)] public UUISceneFSM* SceneFSM_;
            [FieldOffset(0x0410)] public UCmpCommuList* CommuListScene_;
            //[FieldOffset(0x0418)] public UCmpCommuDetails* CommuDetailsScene_;
            [FieldOffset(0x420)] public long UnlockedCount;
            [FieldOffset(0x428)] public TArray<nint> UnlockedCmmEntries;
            [FieldOffset(0x438)] public TArray<nint> CmmEntries2;
            [FieldOffset(0x448)] public CmpCommuMenu* Menu;
            [FieldOffset(0x0458)] public ACmpMainActor* pMainActor;
            //[FieldOffset(0x0460)] public UCmpCommu* pParent;
            //[FieldOffset(0x0468)] public ACmpCommuModelController* pModelController;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x550)]
        public unsafe struct UCmpCommuList
        {
            //[FieldOffset(0x0000)] public UUIScene baseObj;
            [FieldOffset(0xa8)] public FVector2D CursorOffsetTemp;
            [FieldOffset(0x148)] public FVector2D CursorOffsetPermanent;
            // [FieldOffset(0x1a0)] public UCmpCommuListVisibleEntries VisibleEntries[5];
            [FieldOffset(0x04C0)] public AUICmpCommu* Context_;
            [FieldOffset(0x04C8)] public AUICmpCommu* pParent;
            [FieldOffset(0x04D0)] public ACmpMainActor* pMainActor;

            public UCmpCommuListVisibleEntries* GetListVisibleEntry(int id)
            {
                if (id < 0 || id > 4) return null;
                fixed (UCmpCommuList* self = &this) { return &((UCmpCommuListVisibleEntries*)((nint)self + 0x1a0))[id]; }
            }
        }
        private unsafe void UCmpCommuList_DrawSocialLinkListImpl(UCmpCommuList* self, int drawId, USprAsset* campSpr)
        {
            var cmpCommuListMenu = self->pParent->Menu;
            var unlockedCmms = &self->pParent->UnlockedCmmEntries;
            float listOpacity = (self->GetListVisibleEntry(0)->Flags != 0) ? self->GetListVisibleEntry(0)->Opacity : 0.0f;
            // list title
            var commuListTitleLayout = self->pMainActor->OthersLayoutDataTable->GetLayoutDataTableEntry(0x36); // DT_UILayout_CampOthers->COMMU_LIST_TITLE
            FVector2D commuListTitlePos = (commuListTitleLayout != null) ? commuListTitleLayout->position : new FVector2D(103, -50);
            var listXposAnchor = self->GetListVisibleEntry(0)->Field34 + 83;
            _drawSprDetailedParams.Invoke(campSpr, 0, 0x327, 
                commuListTitlePos.X + listXposAnchor, commuListTitlePos.Y + 257, 0,
                listColors.ListTitleColor, drawId, 0, 1, 1, 4, 1
            );
            // new FSprColor(0xff, 0xff, 0xff, (byte)(listOpacity * 255))
            // community list cursor
            _cmpDrawQuad.Invoke(cmmListEntryPoints, 
                self->CursorOffsetPermanent.X + self->CursorOffsetTemp.X + listXposAnchor + 316,
                self->CursorOffsetPermanent.Y + self->CursorOffsetTemp.Y + 320, 
                0,
                listColors.CursorColor, drawId, 0, 1.5f);
            for (int i = 0; i < cmpCommuListMenu->EntriesVisibleAtOnce; i++)
            {
                FVector2D cmmListEntryPos = new FVector2D(i * 25 + self->GetListVisibleEntry(i)->Field34 + 83, i * 150 + 257);
                var currCmd = unlockedCmms->Get<CmmPtr>(i + cmpCommuListMenu->ScrollEntryOffset);
                _cmpDrawQuad.Invoke(cmmListEntryPoints, // background
                    cmmListEntryPos.X + 303, 
                    68 + cmmListEntryPos.Y, 0,
                    listColors.GetBackgroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, 1.5f);
                _drawRoundRect.Invoke( // name plate
                    cmmListEntryPos.X + 36.5f, 
                    79 + cmmListEntryPos.Y, 0,
                    539, 50, 3,
                    listColors.GetForegroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, 0, 0
                );
                _drawSprDetailedParams.Invoke( // sl card border
                    campSpr, 0, 0x2ef,
                    cmmListEntryPos.X + 52,
                    48 + cmmListEntryPos.Y, 0,
                    listColors.GetForegroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, 1, 1, 4, 1
                );
                _drawSprDetailedParams.Invoke( // sl card inside
                    campSpr, 0, 0x2ee,
                    cmmListEntryPos.X + 52,
                    48 + cmmListEntryPos.Y, 0,
                    listColors.GetBackgroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, 1, 1, 4, 1
                );
                uint slCardArcanaId = (i + cmpCommuListMenu->ScrollEntryOffset < SocialLinkManager.vanillaCmmLimit)
                    ? 0x2d4 + (uint)currCmd->ArcanaId
                    : 0x2d6;
                _drawSprDetailedParams.Invoke( // sl card arcana
                    campSpr, 0, slCardArcanaId,
                    cmmListEntryPos.X + 55,
                    46 + cmmListEntryPos.Y, 0,
                    listColors.GetForegroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, 1, 1, 4, 1
                );
                var commuListArcanaName = self->pMainActor->OthersLayoutDataTable->GetLayoutDataTableEntry(6); // DT_UILayout_CampOthers->COMMU_LIST_ARCANA_NAME
                FVector2D commuListArcanaNamePos = (commuListArcanaName != null) ? commuListArcanaName->position : new FVector2D(94, 8);
                float commuListArcanaNameSize = (commuListArcanaName != null) ? commuListArcanaName->scale : 0.72f;
                _drawSprDetailedParams.Invoke(
                    campSpr, 0, 0x2f2 + (uint)currCmd->ArcanaId,
                    commuListArcanaNamePos.X + cmmListEntryPos.X,
                    commuListArcanaNamePos.Y + cmmListEntryPos.Y, 0,
                    listColors.GetForegroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, commuListArcanaNameSize, commuListArcanaNameSize, 0, 1
                );
                if (currCmd->entry->Rank < 10)
                {
                    _drawSprDetailedParams.Invoke(
                        campSpr, 0, 0x30c,
                        442 + cmmListEntryPos.X,
                        63 + cmmListEntryPos.Y, 0,
                        listColors.GetForegroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, 0.9f, 0.9f, 4, 1
                    );
                }
                uint slArcanaRankId = (currCmd->entry->Rank < 10) ? 0x30c + (uint)currCmd->entry->Rank : 0x325;
                var slArcanaRankSize = (currCmd->entry->Rank < 10) ? 0.45f : 1;
                var slArcanaRankPos = (currCmd->entry->Rank < 10) ? new FVector2D(535, 47) : new FVector2D(486, 47);
                _drawSprDetailedParams.Invoke(
                    campSpr, 0, slArcanaRankId,
                    slArcanaRankPos.X + cmmListEntryPos.X,
                    slArcanaRankPos.Y + cmmListEntryPos.Y, 0,
                    listColors.GetForegroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, slArcanaRankSize, slArcanaRankSize, 4, 1
                );
                // DT_UILayout_CampOthers->COMMU_LIST_NAME_TEXT
                // DT_UILayout_CampCommuTextCol->COMMU_NAME_LIST
                //_context._utils.Log($"{i}: {currCmd->ArcanaId}");
            }
            //_drawSocialLinkList.OriginalFunction(self, drawId, campSpr);
        }
    }
}
