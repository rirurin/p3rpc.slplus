using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using p3rpc.slplus.Interfaces;
using p3rpc.slplus.Messages;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using System.Runtime.InteropServices;
using static p3rpc.slplus.Messages.MessageHooks;
using static p3rpc.slplus.SocialLink.SocialLinkManager;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace p3rpc.slplus.SocialLink
{
    public class CommuListColors : ICommuListColors
    {
        public static readonly ConfigColor DarkColorDefault = new ConfigColor(0xe, 0xe, 0x54, 0xff);
        public static readonly ConfigColor LightColorDefault = new ConfigColor(0x72, 0xff, 0xff, 0xff);
        public static readonly ConfigColor Red = new ConfigColor(0xff, 0x0, 0x0, 0xff);
        public static readonly ConfigColor defaultDetailsBgBox = new ConfigColor(0x0, 0x1, 0x58, 0xff);
        public static readonly ConfigColor defaultDetailsNameplateTriangle = new ConfigColor(0x6, 0x0, 0xfd, 0xff);
        public static readonly ConfigColor defaultDetailsNameSprite = new ConfigColor(0xe, 0xe2, 0xf8, 0xff);
        public ConfigColor BgColorNormal { get; private set; } = DarkColorDefault;
        public ConfigColor BgColorSelected { get; private set; } = ConfigColor.White;
        // public FSprColor BgColorReverse { get; set; } = new FSprColor(0x0, 0x0, 0x0, 0xff); (Not implemented in P3RE)
        public ConfigColor FgColorNormal { get; private set; } = LightColorDefault;
        public ConfigColor FgColorReverse { get; private set; } = Red;
        public ConfigColor FgColorSelected { get; private set; } = ConfigColor.Black;
        public ConfigColor CursorColor { get; private set; } = Red;
        public ConfigColor ListTitleColor { get; private set; } = ConfigColor.White;
        public ConfigColor DetailsBgBox { get; private set; } = defaultDetailsBgBox;
        public ConfigColor DetailsNameplateTriangle { get; private set; } = defaultDetailsNameplateTriangle;
        public ConfigColor DetailsNameSprite { get; private set; } = defaultDetailsNameSprite;
        public unsafe FSprColor GetBackgroundColorFromMenu(CampMenuHooks.CmpCommuMenu* menu, int id)
            => ConfigColor.ToFSprColor((menu->VisibleEntryOffset == id) ? (BgColorSelected) : BgColorNormal);

        public unsafe FSprColor GetForegroundColorFromMenu(CampMenuHooks.CmpCommuMenu* menu, int id)
            => ConfigColor.ToFSprColor((menu->VisibleEntryOffset == id) ? FgColorSelected : FgColorNormal);

        // implement ICommuListColors
        public void SetBgColorNormal(byte r, byte g, byte b, byte a) => BgColorNormal = new ConfigColor(r, g, b, a);
        public void SetBgColorSelected(byte r, byte g, byte b, byte a) => BgColorSelected = new ConfigColor(r, g, b, a);
        public void SetFgColorNormal(byte r, byte g, byte b, byte a) => FgColorNormal = new ConfigColor(r, g, b, a);
        public void SetFgColorReverse(byte r, byte g, byte b, byte a) => FgColorReverse = new ConfigColor(r, g, b, a);
        public void SetFgColorSelected(byte r, byte g, byte b, byte a) => FgColorSelected = new ConfigColor(r, g, b, a);
        public void SetCursorColor(byte r, byte g, byte b, byte a) => CursorColor = new ConfigColor(r, g, b, a);
        public void SetListTitleColor(byte r, byte g, byte b, byte a) => ListTitleColor = new ConfigColor(r, g, b, a);
        public void SetDetailsBgBox(byte r, byte g, byte b, byte a) => DetailsBgBox = new ConfigColor(r, g, b, a);
        public void SetDetailsNameplateTriangle(byte r, byte g, byte b, byte a) => DetailsNameplateTriangle = new ConfigColor(r, g, b, a);
        public void SetDetailsNameSprite(byte r, byte g, byte b, byte a) => DetailsNameSprite = new ConfigColor(r, g, b, a);
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

        private string AUIDrawBaseActor_DrawFont_SIG = "4C 8B DC 49 89 5B ?? 49 89 73 ?? 55 49 8D AB ?? ?? ?? ??";
        private string AUIDrawBaseActor_DrawFont2_SIG = "4C 8B DC 53 55 48 81 EC A8 01 00 00";
        private AUIDrawBaseActor_DrawFont _drawFont;
        private AUIDrawBaseActor_DrawFont _drawFont2;
        public unsafe delegate void AUIDrawBaseActor_DrawFont(TextData* text, nint a2, float a3, float a4, float a5, float a6, byte a7, int a8);

        private string UCmpCommuDetails_SocialLinkDrawDescription_SIG = "40 55 56 41 55 41 56 48 8D 6C 24 ?? 48 81 EC 68 01 00 00";
        private IHook<UCmpCommuDetails_SocialLinkDrawDescription> _slDrawDescription;
        public unsafe delegate void UCmpCommuDetails_SocialLinkDrawDescription(UCmpCommuDetails* self, int drawId);

        private string AUICmpCommu_CmmOutlineHelpGetDialog_SIG = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 49 8B C9";
        private AUICmpCommu_CmmOutlineHelpGetDialog _cmmoutlineHelpGetDialog;
        public unsafe delegate void AUICmpCommu_CmmOutlineHelpGetDialog(AUICmpCommu* self, int* dialogOut, int* pageOut, CmmPtr* cmm);

        private string UCmpCommuDetails_SocialLinkDetailsCharacterDetail_SIG = "40 55 57 41 54 41 55 48 8D AC 24 ?? ?? ?? ?? 48 81 EC 18 02 00 00";
        private IHook<UCmpCommuDetails_SocialLinkDetailsCharacterDetail> _slDetailsCharDetails;
        public unsafe delegate void UCmpCommuDetails_SocialLinkDetailsCharacterDetail(UCmpCommuDetails* self, int drawId, USprAsset* campSpr);

        private string ACmpCommuModelController_Tick_SIG = "40 55 41 56 48 8D 6C 24 ?? 48 81 EC D8 00 00 00 44 0F 29 8C 24 ?? ?? ?? ??";
        private IHook<ACmpCommuModelController_Tick> _cmpCommuModelCtrl;
        public unsafe delegate void ACmpCommuModelController_Tick(ACmpCommuModelController* self, float dt);

        private string AUICmpCommu_SetArcanaTexId_SIG = "48 8B 83 ?? ?? ?? ?? 48 85 C0 0F 84 ?? ?? ?? ?? 8B 40 ?? 33 ED";
        private IAsmHook _setArcanaTexId;
        private IReverseWrapper<AUICmpCommu_SetArcanaTexId> _setArcanaTexIdWrapper;
        [Function(new Register[] { FunctionAttribute.Register.r8, FunctionAttribute.Register.rdi }, FunctionAttribute.Register.rax, true)]
        public unsafe delegate int AUICmpCommu_SetArcanaTexId(AUICmpCommu* self, int arcanaOg);

        //private string ACmpCommuModelController_SetArcanaTexId_SIG = "48 8D 15 ?? ?? ?? ?? 49 8B 8E ?? ?? ?? ??";
        private string ACmpCommuModelController_SetArcanaTexId_SIG = "4A 8B 1C ?? 41 B8 01 00 00 00";
        private IAsmHook _setArcanaTexIdSwitch;
        private IReverseWrapper<ACmpCommuModelController_SetArcanaTexId> _setArcanaTexIdSwitchWrapper;
        [Function(FunctionAttribute.Register.r8, FunctionAttribute.Register.r8, true)]
        public unsafe delegate int ACmpCommuModelController_SetArcanaTexId(int rax);

        private string AUICmpCommu_SocialLinkDetailsDrawHeaderArcana_SIG = "49 8B 8F ?? ?? ?? ?? BA 08 00 00 00";
        private IAsmHook _drawHeaderArcana;
        private IReverseWrapper<AUICmpCommu_SocialLinkDetailsDrawHeaderArcana> _drawHeaderArcanaWrapper;
        [Function(FunctionAttribute.Register.rax, FunctionAttribute.Register.rax, true)]
        public unsafe delegate int AUICmpCommu_SocialLinkDetailsDrawHeaderArcana(int ogArcana);

        private string AUIDrawBaseActor_DrawTriangle_SIG = "48 8B C4 4C 89 48 ?? 4C 89 40 ?? 48 89 50 ?? 48 89 48 ?? 55 53 56 57 41 57";
        private AUIDrawBaseActor_DrawTriangle _drawTriangle;
        public unsafe delegate void AUIDrawBaseActor_DrawTriangle(BPDrawSpr* drawer, FVector* p0, FVector* p1, FVector* p2, FColor color, float ScaleX, float ScaleY, float Angle, float Antialiasing, byte a11, byte drawId);

        private string SetPresetBlendState_140cc8540_SIG = "48 83 EC 58 83 FA 09";
        private SetPresetBlendState_140cc8540 _setPresetBlendState;
        public unsafe delegate void SetPresetBlendState_140cc8540(BPDrawSpr* drawer, EUIOTPRESET_BLEND_TYPE type, uint drawId);

        private string UCmpCommu_DetailsDrawPortrait_SIG = "48 8B C4 48 89 58 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 68 ?? 48 81 EC 40 01 00 00 0F 29 70 ?? 8B FA";
        private IHook<UCmpCommu_DetailsDrawPortrait> _detailsDrawPortrait;
        public unsafe delegate void UCmpCommu_DetailsDrawPortrait(UCmpCommu* self, uint drawId);

        private string DrawTexture_SIG = "E8 ?? ?? ?? ?? 45 84 F6 75 ?? 45 84 FF 0F 84 ?? ?? ?? ?? 48 8B 43 ??";
        private DrawTexture _drawTexture;
        public unsafe delegate void DrawTexture(BPDrawSpr* drawer, float x, float y, float z, float texSizeX, float texSizeY, FSprColor* texColor, float sizeX, float sizeY, float angle, FVector2D* texCoord0, FVector2D* texCoord1, UTexture2D* texture, int drawPoints, int drawId);

        private string UCmmProfileDataAsset_GetBmdProfilePage_SIG = "40 55 53 56 41 55 48 8B EC 48 83 EC 38";
        private UCmmProfileDataAsset_GetBmdProfilePage _getBmdProfilePage;
        public unsafe delegate int UCmmProfileDataAsset_GetBmdProfilePage(UCmmProfileDataAsset* self, short arcana, short modelId);

        private unsafe FVector2D* cmmListEntryPoints;
        private unsafe FVector2D* cmmListEntryPointsScrollTrack;

        private unsafe AActor* actorDefaultInstance;

        private unsafe UUIResources* uiResourcesInstance;

        public CommuListColors listColors = new();

        private CommonHooks _common;
        private MessageHooks _msgHook;
        private SocialLinkManager _manager;
        private SocialLinkUtilities _utils;
        public unsafe CampMenuHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UCmpCommuList_DrawSocialLinkList_SIG, "UCmpCommuList::DrawSocialLinkList", _context._utils.GetIndirectAddressShort,
                addr => _drawSocialLinkList = _context._utils.MakeHooker<UCmpCommuList_DrawSocialLinkList>(UCmpCommuList_DrawSocialLinkListImpl, addr));

            _context._utils.SigScan(DrawSpriteDetailedParams_SIG, "DrawSpriteDetailedParams", _context._utils.GetDirectAddress, addr => _drawSprDetailedParams = _context._utils.MakeWrapper<DrawSpriteDetailedParams>(addr));
            _context._utils.SigScan(UCmpCommuList_DrawQuad_SIG, "UCmpCommuList::DrawQuad", _context._utils.GetDirectAddress, addr => _cmpDrawQuad = _context._utils.MakeWrapper<UCmpCommuList_DrawQuad>(addr));
            _context._utils.SigScan(UCmpCommuList_DrawRoundRect_SIG, "UCmpCommuList::DrawRoundRect", _context._utils.GetDirectAddress, addr => _drawRoundRect = _context._utils.MakeWrapper<UCmpCommuList_DrawRoundRect>(addr));
            _context._utils.SigScan(AUIDrawBaseActor_DrawFont_SIG, "AUIDrawBaseActor::DrawFont", _context._utils.GetDirectAddress, addr => _drawFont = _context._utils.MakeWrapper<AUIDrawBaseActor_DrawFont>(addr));
            _context._utils.SigScan(AUIDrawBaseActor_DrawFont2_SIG, "AUIDrawBaseActor::DrawFont2", _context._utils.GetDirectAddress, addr => _drawFont2 = _context._utils.MakeWrapper<AUIDrawBaseActor_DrawFont>(addr));
            _context._utils.SigScan(AUIDrawBaseActor_FontDrawEx_SIG, "AUIDrawBaseActor::FontDrawEx", _context._utils.GetIndirectAddressShort, addr => _fontDrawEx = _context._utils.MakeWrapper<AUIDrawBaseActor_FontDrawEx>(addr));

            _context._utils.SigScan(UCmpCommuDetails_SocialLinkDrawDescription_SIG, "UCmpCommuDetails::SocialLinkDrawDescription", _context._utils.GetDirectAddress,
                addr => _slDrawDescription = _context._utils.MakeHooker<UCmpCommuDetails_SocialLinkDrawDescription>(UCmpCommuDetails_SocialLinkDrawDescriptionImpl, addr));
            _context._utils.SigScan(AUICmpCommu_CmmOutlineHelpGetDialog_SIG, "AUICmpCommu::CmmOutlineHelpGetDialog", _context._utils.GetDirectAddress, addr => _cmmoutlineHelpGetDialog = _context._utils.MakeWrapper<AUICmpCommu_CmmOutlineHelpGetDialog>(addr));
            _context._utils.SigScan(UCmpCommuDetails_SocialLinkDetailsCharacterDetail_SIG, "UCmpCommuDetails::SocialLinkDetailsCharacterDetail", _context._utils.GetDirectAddress,
                addr => _slDetailsCharDetails = _context._utils.MakeHooker<UCmpCommuDetails_SocialLinkDetailsCharacterDetail>(UCmpCommuDetails_SocialLinkDetailsCharacterDetailImpl, addr));
            _context._utils.SigScan(ACmpCommuModelController_Tick_SIG, "ACmpCommuModelController::Tick", _context._utils.GetDirectAddress,
                addr => _cmpCommuModelCtrl = _context._utils.MakeHooker<ACmpCommuModelController_Tick>(ACmpCommuModelController_TickImpl, addr));

            _context._utils.SigScan(AUICmpCommu_SetArcanaTexId_SIG, "AUICmpCommu::SetArcanaTexId", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"{_context._utils.PreserveMicrosoftRegisters()}",
                    $"{_context._hooks.Utilities.GetAbsoluteCallMnemonics(AUICmpCommu_SetArcanaTexIdImpl, out _setArcanaTexIdWrapper)}",
                    $"mov edi, eax",
                    $"{_context._utils.RetrieveMicrosoftRegisters()}",
                };
                _setArcanaTexId = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            /*
             * this hook causes a CLR error crash every time lol
            _context._utils.SigScan(ACmpCommuModelController_SetArcanaTexId_SIG, "ACmpCommuModelController::SetArcanaTexId", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"push rcx",
                    $"push rdx",
                    $"push r8",
                    $"{_context._hooks.Utilities.GetAbsoluteCallMnemonics(ACmpCommuModelController_SetArcanaTexIdImpl, out _setArcanaTexIdSwitchWrapper)}",
                    $"pop r8",
                    $"pop rdx",
                    $"pop rcx",
                };
                _setArcanaTexIdSwitch = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            */

            _context._utils.SigScan(AUICmpCommu_SocialLinkDetailsDrawHeaderArcana_SIG, "AUICmpCommu::SocialLinkDetailsDrawHeaderArcana", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"{_context._hooks.Utilities.GetAbsoluteCallMnemonics(AUICmpCommu_SocialLinkDetailsDrawHeaderArcanaImpl, out _drawHeaderArcanaWrapper)}",
                };
                _drawHeaderArcana = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(AUIDrawBaseActor_DrawTriangle_SIG, "AUIDrawBaseActor::DrawTriangle", _context._utils.GetDirectAddress, 
                addr => _drawTriangle = _context._utils.MakeWrapper<AUIDrawBaseActor_DrawTriangle>(addr));
            _context._utils.SigScan(SetPresetBlendState_140cc8540_SIG, "SetPresetBlendState::140cc8540", _context._utils.GetDirectAddress, 
                addr => _setPresetBlendState = _context._utils.MakeWrapper<SetPresetBlendState_140cc8540>(addr));
            _context._utils.SigScan(UCmpCommu_DetailsDrawPortrait_SIG, "UCmpCommu::DetailsDrawPortrait", _context._utils.GetDirectAddress, 
                addr => _detailsDrawPortrait = _context._utils.MakeHooker<UCmpCommu_DetailsDrawPortrait>(UCmpCommu_DetailsDrawPortraitImpl, addr));
            _context._utils.SigScan(DrawTexture_SIG, "DrawTexture", _context._utils.GetIndirectAddressShort, 
                addr => _drawTexture = _context._utils.MakeWrapper<DrawTexture>(addr));
            _context._utils.SigScan(UCmmProfileDataAsset_GetBmdProfilePage_SIG, "UCmmProfileDataAsset::GetBmdProfilePage", _context._utils.GetDirectAddress,
                addr => _getBmdProfilePage = _context._utils.MakeWrapper<UCmmProfileDataAsset_GetBmdProfilePage>(addr));

            cmmListEntryPoints = (FVector2D*)NativeMemory.Alloc((nuint)sizeof(FVector2D) * 4);
            // Cmm entry blocks
            cmmListEntryPoints[0].X = -303.0f;
            cmmListEntryPoints[0].Y = -68.0f;
            cmmListEntryPoints[1].X = 280.0f;
            cmmListEntryPoints[1].Y = -68.0f;
            cmmListEntryPoints[2].X = -280.0f;
            cmmListEntryPoints[2].Y = 69.0f;
            cmmListEntryPoints[3].X = 303.0f;
            cmmListEntryPoints[3].Y = 69.0f;
            // Scrollbar track
            cmmListEntryPointsScrollTrack = (FVector2D*)NativeMemory.Alloc((nuint)sizeof(FVector2D) * 4);
            cmmListEntryPointsScrollTrack[0].X = -6.0f;
            cmmListEntryPointsScrollTrack[0].Y = -6.0f;
            cmmListEntryPointsScrollTrack[1].X = 17.0f;
            cmmListEntryPointsScrollTrack[1].Y = -6.0f;
            cmmListEntryPointsScrollTrack[2].X = 28.0f;
            cmmListEntryPointsScrollTrack[2].Y = 202.0f;
            cmmListEntryPointsScrollTrack[3].X = 51.0f;
            cmmListEntryPointsScrollTrack[3].Y = 202.0f;
        }
        public override void Register()
        {
            _manager = GetModule<SocialLinkManager>();
            _utils = GetModule<SocialLinkUtilities>();
            _common = GetModule<CommonHooks>();
            _msgHook = GetModule<MessageHooks>();
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
            [FieldOffset(0x288)] public int Field288;
            //[FieldOffset(0x0408)] public UUISceneFSM* SceneFSM_;
            [FieldOffset(0x0410)] public UCmpCommuList* CommuListScene_;
            [FieldOffset(0x0418)] public UCmpCommuDetails* CommuDetailsScene_;
            [FieldOffset(0x420)] public long UnlockedCount;
            [FieldOffset(0x428)] public TArray<nint> UnlockedCmmEntries;
            [FieldOffset(0x438)] public TArray<nint> CmmEntries2;
            [FieldOffset(0x448)] public CmpCommuMenu* Menu;
            [FieldOffset(0x0458)] public ACmpMainActor* pMainActor;
            [FieldOffset(0x0460)] public UCmpCommu* pParent;
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

        // also used in p3rpc research
        [StructLayout(LayoutKind.Explicit, Size = 0xa4)]
        public unsafe struct TextData
        {
            [FieldOffset(0x0)] public FVector Position;
            [FieldOffset(0xc)] public FSprColor Color;
            [FieldOffset(0x10)] public float Size;
            [FieldOffset(0x18)] public FString Text;
            [FieldOffset(0x2c)] public FVector BasePosition;
            [FieldOffset(0x38)] public float BaseSize;
            [FieldOffset(0x40)] public float Angle;
            [FieldOffset(0x60)] public FVector4 TransformMatrixRow0;
            [FieldOffset(0x70)] public FVector4 TransformMatrixRow1;
            [FieldOffset(0x80)] public FVector4 TransformMatrixRow2;
            [FieldOffset(0x90)] public FVector4 TransformMatrixRow3;
            [FieldOffset(0xa0)] public int FieldA0;

            public TextData(FVector position, FSprColor color, float size, FString text)
            {
                Position = position;
                Color = color;
                Size = size;
                Text = text;
                BasePosition = new FVector(0, 0, 0);
                BaseSize = 1;
                Angle = 0;
                TransformMatrixRow0 = new FVector4(1, 0, 0, 0);
                TransformMatrixRow1 = new FVector4(0, 1, 0, 0);
                TransformMatrixRow2 = new FVector4(0, 0, 1, 0);
                TransformMatrixRow3 = new FVector4(0, 0, 0, 1);
                FieldA0 = 1;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x968)]
        public unsafe struct UCmpCommuDetails
        {
            //[FieldOffset(0x0000)] public UUIScene baseObj;
            [FieldOffset(0x40)] public BPDrawSpr Drawer;
            [FieldOffset(0x0060)] public AUICmpCommu* Context_;
            [FieldOffset(0x68)] public CmpCommuMenu* Menu;
            [FieldOffset(0x78)] public TArray<FCommunityFormattedName> FormattedNames;
            [FieldOffset(0x1fc)] public float Field1FC;
            [FieldOffset(0x47c)] public float CharDetailBgOffsetX;
            [FieldOffset(0x08B0)] public AUICmpCommu* pParent;
            [FieldOffset(0x08B8)] public ACmpMainActor* pMainActor;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x2A0)]
        public unsafe struct AAppPropsCore
        {
            [FieldOffset(0x0000)] public AAppActor baseObj;
            [FieldOffset(0x0278)] public FName mOnFlagName_;
            [FieldOffset(0x0280)] public FName mOffFlagName_;
            //[FieldOffset(0x0288)] public USceneComponent* Root;
            //[FieldOffset(0x0290)] public USkeletalMeshComponent* SkeletalMesh;
            //[FieldOffset(0x0298)] public UAppPropsAnimPackAsset* mAnimePackAsset_;
        }

        public enum EAppPropsCardType
        {
            Blank = 0,
            Persona = 1,
            MajorArcana = 2,
            MinorArcana = 3,
        };


        [StructLayout(LayoutKind.Explicit, Size = 0xC)]
        public unsafe struct FAppPropsCardParam
        {
            [FieldOffset(0x0000)] public EAppPropsCardType Type;
            [FieldOffset(0x0004)] public int ID;
            [FieldOffset(0x0008)] public int Rank;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x18)]
        public unsafe struct FAppPropsCardData
        {
            [FieldOffset(0x0000)] public FAppPropsCardParam Param;
            [FieldOffset(0x0010)] public AAppPropsCore* Card;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
        public unsafe struct AAppPropsCardContainer
        {
            [FieldOffset(0x0000)] public AAppActor baseObj;
            [FieldOffset(0x0278)] public UAssetLoader* Loader;
            [FieldOffset(0x0288)] public TArray<FAppPropsCardData> CardList;
            [FieldOffset(0x0298)] public UClass* PersonaCardClass; // TSubclassOf<AAppPropsCore>
            [FieldOffset(0x02A0)] public UClass* MajorCardClass; // TSubclassOf<AAppPropsCore>
            [FieldOffset(0x02A8)] public UClass* MinorCardClass; // TSubclassOf<AAppPropsCore>
        }


        [StructLayout(LayoutKind.Explicit, Size = 0x2E8)]
        public unsafe struct ACmpCommuModelController
        {
            [FieldOffset(0x0000)] public AAppActor baseObj;
            [FieldOffset(0x2a0)] public TArray<ArcanaCardIdTex> ArcanaCardId;
            [FieldOffset(0x02B0)] public ACmpMainActor* pMainActor;
            [FieldOffset(0x02B8)] public AAppPropsCardContainer* pCardContainer;
            [FieldOffset(0x02C0)] public AAppPropsCore* pCardBp;
            [FieldOffset(0x02C8)] public TArray<IntPtr> pTextures;
            [FieldOffset(0x02D8)] public TArray<IntPtr> pMotions;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xC60)]
        public unsafe struct UCmpCommu
        {
            [FieldOffset(0x0000)] public UCmpMenuBase baseObj;
            [FieldOffset(0x38)] public BPDrawSpr drawer;
            [FieldOffset(0x48)] public int MenuSelected;
            [FieldOffset(0x4c)] public int MenuState;
            [FieldOffset(0x58)] public byte bIsInitialized;
            [FieldOffset(0x0060)] public UTexture2D* pCommuBustupAry;
            [FieldOffset(0x0C48)] public UAssetLoader* AssetLoader_;
            [FieldOffset(0x0C50)] public AUICmpCommu* Actor_;
            [FieldOffset(0x0C58)] public ACmpCommuModelController* pModelController;

            public UTexture2D* GetCustomSlPortrait(int cmmId)
            {
                fixed (UCmpCommu* self = &this)
                {
                    return ((UCmpCommuExtendedEntry*)(self + 1))[cmmId - vanillaCmmLimit - 1].BustupTex;
                    //return *(UTexture2D**)((nint)(self + 1) + (cmmId - SocialLinkManager.vanillaCmmLimit - 1) * 8);
                }
            }

            public UCmpCommuExtendedEntry* GetExtendedCmmEntry(int cmmId)
            {
                fixed (UCmpCommu* self = &this)
                {
                    return &((UCmpCommuExtendedEntry*)(self + 1))[cmmId - vanillaCmmLimit - 1];
                }
            }
        }
        [StructLayout(LayoutKind.Explicit, Size = 0x8)]
        public unsafe struct ArcanaCardIdTex
        {
            [FieldOffset(0x0)] public int ArcanaId;
            [FieldOffset(0x4)] public int Field04;
        }

        public enum ECmmProfileMsgType
        {
            Normal = 0,
            Reverse = 1,
            Missing = 2,
            Lost = 3,
        };

        [StructLayout(LayoutKind.Explicit, Size = 0x60)]
        public unsafe struct FCmmProfileItem
        {
            [FieldOffset(0x0000)] public ushort PCID;
            [FieldOffset(0x0004)] public uint DisappearID;
            [FieldOffset(0x0008)] public int NameMsgNo;
            //[FieldOffset(0x0010)] public TMap<ECmmProfileMsgType, int> ProfileMsgNo;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x80)]
        public unsafe struct UCmmProfileDataAsset
        {
            //[FieldOffset(0x0000)] public UAppDataAsset baseObj;
            [FieldOffset(0x0030)] public TMap<ushort, FCmmProfileItem> Data;
        }
        private unsafe uint GetArcanaNumberSprId(int arcanaId) 
        {
            if (_manager.cmmIndexToSlHash.TryGetValue(arcanaId, out var arcanaHash) &&
                _manager.activeSocialLinks.TryGetValue(arcanaHash, out var customSl))
                return 0x2d4 + (uint)customSl.ArcanaId;
            else return (arcanaId <= SocialLinkManager.vanillaCmmLimit) ? 0x2d4 + (uint)arcanaId : 0x2d4;
        }
        // NOTE: input is zero indexed
        private unsafe uint GetArcanaNameSprId(int arcanaId)
        {
            if (_manager.cmmIndexToSlHash.TryGetValue(arcanaId, out var arcanaHash) &&
                _manager.activeSocialLinks.TryGetValue(arcanaHash, out var customSl))
                return 0x2f2 + (uint)customSl.ArcanaId;
            else return (arcanaId <= SocialLinkManager.vanillaCmmLimit) ? 0x2f2 + (uint)arcanaId : 0x2f2;
        }
        private unsafe void UCmpCommuList_DrawSocialLinkListImpl(UCmpCommuList* self, int drawId, USprAsset* campSpr)
        {
            //_context._utils.Log($"camp spr asset: 0x{(nint)campSpr:X}");
            var cmpCommuListMenu = self->pParent->Menu;
            var unlockedCmms = &self->pParent->UnlockedCmmEntries;
            float listOpacity = (self->GetListVisibleEntry(0)->Flags != 0) ? self->GetListVisibleEntry(0)->Opacity : 0.0f;
            // list title
            var commuListTitleLayout = self->pMainActor->OthersLayoutDataTable->GetLayoutDataTableEntry(0x36); // DT_UILayout_CampOthers->COMMU_LIST_TITLE
            FVector2D commuListTitlePos = (commuListTitleLayout != null) ? commuListTitleLayout->position : new FVector2D(103, -50);
            var listXposAnchor = self->GetListVisibleEntry(0)->Field34 + 83;
            _drawSprDetailedParams.Invoke(campSpr, 0, 0x327, 
                commuListTitlePos.X + listXposAnchor, commuListTitlePos.Y + 257, 0,
                ConfigColor.ToFSprColor(listColors.ListTitleColor), drawId, 0, 1, 1, 4, 1
            );
            // new FSprColor(0xff, 0xff, 0xff, (byte)(listOpacity * 255))
            // community list cursor
            _cmpDrawQuad.Invoke(cmmListEntryPoints, 
                self->CursorOffsetPermanent.X + self->CursorOffsetTemp.X + listXposAnchor + 316,
                self->CursorOffsetPermanent.Y + self->CursorOffsetTemp.Y + 320, 
                0,
                ConfigColor.ToFSprColor(listColors.CursorColor), drawId, 0, 1.5f);
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
                /*
                uint slCardArcanaId = (i + cmpCommuListMenu->ScrollEntryOffset < SocialLinkManager.vanillaCmmLimit)
                    ? 0x2d4 + (uint)currCmd->ArcanaId
                    : 0x2d6;
                */
                _drawSprDetailedParams.Invoke( // sl card arcana
                    campSpr, 0, GetArcanaNumberSprId(currCmd->ArcanaId),
                    cmmListEntryPos.X + 55,
                    46 + cmmListEntryPos.Y, 0,
                    listColors.GetForegroundColorFromMenu(cmpCommuListMenu, i), drawId, 0, 1, 1, 4, 1
                );
                var commuListArcanaName = self->pMainActor->OthersLayoutDataTable->GetLayoutDataTableEntry(6); // DT_UILayout_CampOthers->COMMU_LIST_ARCANA_NAME
                FVector2D commuListArcanaNamePos = (commuListArcanaName != null) ? commuListArcanaName->position : new FVector2D(94, 8);
                float commuListArcanaNameSize = (commuListArcanaName != null) ? commuListArcanaName->scale : 0.72f;
                _drawSprDetailedParams.Invoke(
                    campSpr, 0, GetArcanaNameSprId(currCmd->ArcanaId),
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
                var commuListName = self->pMainActor->OthersLayoutDataTable->GetLayoutDataTableEntry(7); // DT_UILayout_CampOthers->COMMU_LIST_NAME_TEXT
                var commuNameList = self->pMainActor->CommuTextColLayoutDataTable->GetLayoutDataTableEntry(0); // DT_UILayout_CampCommuTextCol->COMMU_NAME_LIST
                FVector2D commuNameListPos = (commuNameList != null) ? commuNameList->position : new FVector2D(0, 0);
                FVector2D commuListNamePos = (commuListName != null) ? commuListName->position : new FVector2D(0, 0);
                FString* arcanaName = _context._memoryMethods.FMemory_Malloc<FString>();
                _manager.UCommunityHandler_GetCommunityNameFromIdImpl(arcanaName, (byte)currCmd->ArcanaId);
                TextData cmmName = new TextData(
                    new FVector(cmmListEntryPos.X + commuListNamePos.X, cmmListEntryPos.Y + commuListNamePos.Y, 0), 
                    listColors.GetBackgroundColorFromMenu(cmpCommuListMenu, i), 1, *arcanaName);
                _drawFont.Invoke(&cmmName, 0, 450, 45, 0, 0, 0, drawId); // commuNameListPos.X, commuNameListPos.Y
                _context._memoryMethods.FMemory_Free(arcanaName->text.allocator_instance);
                _context._memoryMethods.FMemory_Free(arcanaName);
            }
            if (self->pParent->UnlockedCmmEntries.arr_num > 5)
            {
                _cmpDrawQuad.Invoke( // Scrollbar track
                    cmmListEntryPointsScrollTrack,
                    self->GetListVisibleEntry(4)->Field34 + 134, 831, 0,
                    ConfigColor.ToFSprColor(listColors.BgColorNormal), drawId, 0, 1.5f
                );
                // Scrollbar thumb
                float cmmEntryCountInv = 1.0f / self->pParent->UnlockedCmmEntries.arr_num;
                float fScrollEntryOffset = self->pParent->Menu->ScrollEntryOffset;
                var tlX = cmmEntryCountInv * fScrollEntryOffset * 33;
                var tlY = cmmEntryCountInv * fScrollEntryOffset * 196;
                var cmmListEntryPointsScrollThumb = (FVector2D*)NativeMemory.Alloc((nuint)sizeof(FVector2D) * 4);
                cmmListEntryPointsScrollThumb[0].X = tlX;
                cmmListEntryPointsScrollThumb[0].Y = tlY;
                cmmListEntryPointsScrollThumb[1].X = tlX + 12;
                cmmListEntryPointsScrollThumb[1].Y = tlY;
                cmmListEntryPointsScrollThumb[2].X = cmmEntryCountInv * 165 + tlX;
                cmmListEntryPointsScrollThumb[2].Y = cmmEntryCountInv * 980 + tlY;
                cmmListEntryPointsScrollThumb[3].X = cmmEntryCountInv * 165 + tlX + 12;
                cmmListEntryPointsScrollThumb[3].Y = cmmEntryCountInv * 980 + tlY;
                _cmpDrawQuad.Invoke( // Scrollbar track
                    cmmListEntryPointsScrollThumb,
                    self->GetListVisibleEntry(4)->Field34 + 134, 831, 0,
                    ConfigColor.ToFSprColor(listColors.BgColorSelected), drawId, 0, 1.5f
                );
                NativeMemory.Free(cmmListEntryPointsScrollThumb);
            }
        }

        private unsafe void UCmpCommu_DrawTextFromBmd(UBmdAsset* bmd, int dialogNo, int pageNo, FVector textDataPos, FVector drawCallPos, int drawId)
        {
            var bmdWrapper = _msgHook._createBmdWrapper.Invoke(bmd->mBuf.allocator_instance);
            var bmdPage = _msgHook._createBmdPage.Invoke(bmdWrapper, dialogNo, pageNo, 0);
            bmdPage = _msgHook._setPageTextSize.Invoke(bmdPage, 620, 0, 4, 0, 0, 0);
            FString* bmdTextFStr = _context._memoryMethods.FMemory_Malloc<FString>();
            _msgHook._bmdPageToFString.Invoke(bmdTextFStr, (nint)bmdPage->CharacterData->Next);
            TextData bmdText = new TextData(
                textDataPos,
                new FSprColor(0xff, 0xff, 0xff, 0xff), 1, *bmdTextFStr
            );

            _drawFont2.Invoke(&bmdText, 0, drawCallPos.X, drawCallPos.Y, 0, 0, 0, drawId);

            _context._memoryMethods.FMemory_Free(bmdTextFStr);
            _msgHook._freeBmdPage.Invoke(bmdPage);
            _msgHook._freeBmdWrapper.Invoke(bmdWrapper);
        }

        private unsafe void UCmpCommuDetails_SocialLinkDrawDescriptionImpl(UCmpCommuDetails* self, int drawId)
        {
            if (_context._config.ShowOriginalDescription)
            {
                _slDrawDescription.OriginalFunction(self, drawId);
                return;
            }
            var cmmMenu = self->pParent->Menu;
            var currCmd = self->pParent->UnlockedCmmEntries.Get<CmmPtr>(cmmMenu->VisibleEntryOffset + cmmMenu->ScrollEntryOffset);
            if (currCmd != null)
            {
                // GetSubsystem has performance issues so we cache the result (the map's Find function's O(1)?? idk what it's problem is)
                if (uiResourcesInstance == null)
                    uiResourcesInstance = _context._objectMethods.GetSubsystem<UUIResources>((UGameInstance*)_common._getUGlobalWork());
                //var uiResources = _context._objectMethods.GetSubsystem<UUIResources>((UGameInstance*)_common._getUGlobalWork());
                var uiResources = uiResourcesInstance;
                var commuDetailPosX = self->Field1FC;
                var commuDetail = self->pMainActor->CommuTextColLayoutDataTable->GetLayoutDataTableEntry(3); // COMMU_DETAIL
                var commuDetailPosY = self->pMainActor->CommuTextColLayoutDataTable->GetLayoutDataTableEntry(4); // COMMU_DETAIL_POS_Y
                if (uiResources != null)
                {
                    if (currCmd->ArcanaId <= vanillaCmmLimit)
                    {
                        var cmmOutlineHelp = (UBmdAsset*)uiResources->GetAssetEntry(0xd); // Community/Help/BMD_CmmOutlineHelp
                        int dialogNo = 0;
                        int pageNo = 0;
                        _cmmoutlineHelpGetDialog.Invoke(self->pParent, &dialogNo, &pageNo, currCmd);
                        //_context._utils.Log($"[UCmpCommuDetails::SocialLinkDrawDescription] {(nint)cmmOutlineHelp:X}, dialog ID {dialogNo}, page no {pageNo}");
                        UCmpCommu_DrawTextFromBmd(cmmOutlineHelp, dialogNo, pageNo,
                            new FVector(commuDetailPosX + 88, commuDetailPosY->position.Y + 441, 0),
                            new FVector(commuDetail->position.X, commuDetail->position.Y, 0), drawId);
                    }
                }
                if (_manager.cmmIndexToSlHash.TryGetValue(currCmd->ArcanaId, out var cmmHash) && _manager.activeSocialLinks.TryGetValue(cmmHash, out var customSl))
                {
                    if (customSl.CmmOutlineBmd != null)
                    {
                        var customOutlineBmd = self->pParent->pParent->GetExtendedCmmEntry(currCmd->ArcanaId)->OutlineBmd;
                        UCmpCommu_DrawTextFromBmd(customOutlineBmd, 0, currCmd->entry->Rank - 1,
                            new FVector(commuDetailPosX + 88, commuDetailPosY->position.Y + 441, 0),
                            new FVector(commuDetail->position.X, commuDetail->position.Y, 0), drawId);

                    }
                }
            }
        }

        private unsafe void FUN_141301af0(BPDrawSpr* param_1, byte param_2)
        {
            param_1->Flags2 = param_1->Flags2 & 0xfffffffe;
            param_1->Flags2 = param_1->Flags2 | (uint)param_2;
        }

        private unsafe int UCmpCommuDetails_GetVanillaNameSpriteId(int cmmModelId)
        {
            return cmmModelId switch
            {
                2 => 0x349,
                3 => 0x33e,
                4 => 0x33f,
                5 => 0x345,
                6 => 0x344,
                7 => 0x357,
                8 => 0x340,
                9 => 0x341,
                10 => 0x342,
                0x65 => 0x343,
                0x66 => 0x346,
                0x67 => 0x347,
                0x68 => 0x348,
                0x69 => 0x34a,
                0x6a => 0x34e,
                0x6b => 0x34c,
                0x6c => 0x34d,
                0x6d => 0x34b,
                0x6e => 0x34f,
                0x6f => 0x350,
                0x70 => 0x351,
                0x71 => 0x352,
                // CMM_16TOWER______NAME_MUTATSU
                0x72 => _common._getUGlobalWork()->GetBitflag(0x10000289) ? 0x353 : 0x359,
                0x73 => 0x354,
                0x74 => 0x355,
                0x75 => 0x356,
                _ => 0x349
            };
        }
        
        private unsafe void UCmpCommuDetails_DrawNameSprite(UCmpCommuDetails* self, int drawId, USprAsset* campSpr, FVector position)
        {
            var cmmMenu = self->pParent->Menu;
            var currCmd = self->pParent->UnlockedCmmEntries.Get<CmmPtr>(cmmMenu->VisibleEntryOffset + cmmMenu->ScrollEntryOffset)->ArcanaId;
            if (currCmd <= vanillaCmmLimit)
            {
                var cmmDetailsMenu = self->pParent->CommuDetailsScene_->Menu;
                var cmmDetailsFormattedNames = &self->pParent->CommuDetailsScene_->FormattedNames;
                var cmmDetailsSelected = cmmDetailsMenu->ScrollEntryOffset + cmmDetailsMenu->VisibleEntryOffset;
                var cmmModelId = cmmDetailsFormattedNames->arr_num > cmmDetailsSelected ? cmmDetailsFormattedNames->allocator_instance[cmmDetailsSelected].ModelId : 0;
                _drawSprDetailedParams(campSpr, 0, (uint)UCmpCommuDetails_GetVanillaNameSpriteId(cmmModelId), position.X, position.Y, position.Z, 
                    ConfigColor.ToFSprColor(listColors.DetailsNameSprite), drawId, 0, 1, 1, 0, 1);
            } else if (_manager.cmmIndexToSlHash.TryGetValue(currCmd, out var cmmHash) && _manager.activeSocialLinks.TryGetValue(cmmHash, out var customSl))
            {
                var extendedCmmData = self->pParent->pParent->GetExtendedCmmEntry(currCmd);
                if (extendedCmmData->HeaderSpr != null)
                {
                    _drawSprDetailedParams(extendedCmmData->HeaderSpr, 0, 0, position.X, position.Y, position.Z,
                        ConfigColor.ToFSprColor(listColors.DetailsNameSprite), drawId, 0, 1, 1, 0, 1);
                }
            }
        }

        private unsafe void UCmpCommuDetails_SocialLinkDetailsCharacterDetailImpl(UCmpCommuDetails* self, int drawId, USprAsset* campSpr)
        {
            var cmmMenu = self->pParent->Menu;
            var currCmd = self->pParent->UnlockedCmmEntries.Get<CmmPtr>(cmmMenu->VisibleEntryOffset + cmmMenu->ScrollEntryOffset);
            if (currCmd == null) return;
            var uiResources = _context._objectMethods.GetSubsystem<UUIResources>((UGameInstance*)_common._getUGlobalWork());
            var plgHandle = (UPlgAsset*)uiResources->GetAssetEntry(0x33);
            var bgXBase = self->CharDetailBgOffsetX + ((self->pParent->Field288 == 7) ? 967 : 1046); // fVar17
            BPDrawSpr* gDrawer = _common.GetDrawer();
            // background quad
            _setPresetBlendState(gDrawer, EUIOTPRESET_BLEND_TYPE.UI_OT_PRESET_BLEND_MULTRANS, (uint)drawId);
            FUN_141301af0(&self->Drawer, 1);
            FColor detailBgColor = ConfigColor.ToFColorBP(listColors.DetailsBgBox);
            _common._drawPlg(&self->Drawer, bgXBase + 711, 787, 0, &detailBgColor, 0xaa, 1, 1, 0, plgHandle, drawId);
            FUN_141301af0(&self->Drawer, 0);
            // nameplate triangle
            _setPresetBlendState(gDrawer, EUIOTPRESET_BLEND_TYPE.UI_OT_PRESET_BLEND_SEMITRANS, (uint)drawId);
            FVector tlP0 = new FVector(bgXBase + 304, 744, 0);
            FVector tlP1 = new FVector(bgXBase + 221, 867, 0);
            FVector tlP2 = new FVector(bgXBase - 49, 792, 0);
            _drawTriangle(&self->Drawer, &tlP0, &tlP1, &tlP2, ConfigColor.ToFColorBP(listColors.DetailsNameplateTriangle), 1, 1, 0, 1.5f, 0, (byte)drawId);
            // name sprite
            UCmpCommuDetails_DrawNameSprite(self, drawId, campSpr, new FVector(bgXBase, 757, 0));
            // profile help text
            var commuMemberDetail = self->pMainActor->CommuTextColLayoutDataTable->GetLayoutDataTableEntry(8); // COMMU_MEMBER_DETAIL
            var commuMemberDetailY = self->pMainActor->CommuTextColLayoutDataTable->GetLayoutDataTableEntry(9); // COMMU_MEMBER_DETAIL_POSY
            if (currCmd->ArcanaId < vanillaCmmLimit)
            {
                var cmmProfileHelp = (UBmdAsset*)uiResources->GetAssetEntry(0xe); // Community/Help/BMD_CmmProfileHelp
                var cmmDataAsset = (UCmmProfileDataAsset*)uiResources->GetAssetEntry(0xf); // UI/Tables/CmmProfileHelpDataAsset
                var bmdWrapper = _msgHook._createBmdWrapper.Invoke(cmmProfileHelp->mBuf.allocator_instance);
                // get model id
                var detailsMenu = self->pParent->CommuDetailsScene_->Menu;
                var formattedNames = &self->pParent->CommuDetailsScene_->FormattedNames;
                var detailsMenuSel = detailsMenu->ScrollEntryOffset + detailsMenu->VisibleEntryOffset;
                var detailsModelId = (detailsMenuSel < formattedNames->arr_num)
                    ? formattedNames->allocator_instance[detailsMenuSel].ModelId
                    : 0;
                // get page id from arcana + model id
                var bmdProfilePageNum = _getBmdProfilePage(cmmDataAsset, (short)currCmd->ArcanaId, (short)detailsModelId);
                
                UCmpCommu_DrawTextFromBmd(cmmProfileHelp, bmdProfilePageNum, 0, 
                    new FVector(bgXBase + 36, commuMemberDetailY->position.Y + 757, 0),
                    new FVector(commuMemberDetail->position.X, commuMemberDetail->position.Y, 0), drawId);
            }
            if (_manager.cmmIndexToSlHash.TryGetValue(currCmd->ArcanaId, out var cmmHash) && _manager.activeSocialLinks.TryGetValue(cmmHash, out var customSl))
            {
                if (customSl.CmmProfileBmd != null)
                {
                    var customProfileBmd = self->pParent->pParent->GetExtendedCmmEntry(currCmd->ArcanaId)->ProfileBmd;
                    UCmpCommu_DrawTextFromBmd(customProfileBmd, 0, 0,
                        new FVector(bgXBase + 36, commuMemberDetailY->position.Y + 757, 0),
                        new FVector(commuMemberDetail->position.X, commuMemberDetail->position.Y, 0), drawId);
                }
            }
        }

        private unsafe void ACmpCommuModelController_TickImpl(ACmpCommuModelController* self, float dt)
        {
            if (self->ArcanaCardId.arr_num > 0)
            {
                var cardIdInHand = self->ArcanaCardId.GetRef(0);
                var commuActor = ((UCmpCommu*)self->pMainActor->pCurrentMenu)->Actor_;
                if (cardIdInHand->ArcanaId > SocialLinkManager.vanillaCmmLimit)
                {
                    var customSlId = commuActor->UnlockedCmmEntries.Get<CmmPtr>(commuActor->Menu->ScrollEntryOffset + commuActor->Menu->VisibleEntryOffset)->ArcanaId;
                    if (_manager.cmmIndexToSlHash.TryGetValue(customSlId, out var slHash) && _manager.activeSocialLinks.TryGetValue(slHash, out var customSl))
                    {
                        _context._utils.Log($"arcana tex override SWITCH: {cardIdInHand->ArcanaId} -> {customSl.ArcanaId}");
                        cardIdInHand->ArcanaId = (int)customSl.ArcanaId;
                    } else cardIdInHand->ArcanaId = 1;
                }
            }
            _cmpCommuModelCtrl.OriginalFunction(self, dt);
        }

        public unsafe delegate void AActor_Tick(AActor* self, float dt);

        private unsafe int AUICmpCommu_SetArcanaTexIdImpl(AUICmpCommu* self, int arcanaOg)
        {
            if (arcanaOg <= SocialLinkManager.vanillaCmmLimit) return arcanaOg;
            var customSlId = self->UnlockedCmmEntries.Get<CmmPtr>(self->Menu->ScrollEntryOffset + self->Menu->VisibleEntryOffset)->ArcanaId;
            if (_manager.cmmIndexToSlHash.TryGetValue(customSlId, out var slHash) && _manager.activeSocialLinks.TryGetValue(slHash, out var customSl))
            {
                _context._utils.Log($"arcana tex override ENTER: {arcanaOg} -> {customSl.ArcanaId}");
                return (byte)customSl.ArcanaId;
            }
            return 1;
        }
        private unsafe int AUICmpCommu_SocialLinkDetailsDrawHeaderArcanaImpl(int arcanaOg)
        {
            if (arcanaOg <= SocialLinkManager.vanillaCmmLimit) return arcanaOg;
            if (_manager.cmmIndexToSlHash.TryGetValue(arcanaOg, out var slHash) && _manager.activeSocialLinks.TryGetValue(slHash, out var customSl))
            {
                //_context._utils.Log($"header arcana text override: {arcanaOg} -> {customSl.ArcanaId}");
                return (byte)customSl.ArcanaId;
            }
            return 1;
        }

        // FUN_1412930d0
        private unsafe bool UGlobalWork_HasMemberJoined(ushort memberId)
        {
            var gWork = _common._getUGlobalWork();
            return memberId switch
            {
                1 => true,
                >= 2 and <= 10 => gWork->GetBitflag(0x3fffffff + (uint)memberId),
                _ => false
            };
        }
        private unsafe static int BoolToInt(bool a) => (a) ? 1 : 0;

        private unsafe int UCmpCommu_GetPortraitTexIdFool()
        {
            var foolBustupId =
                BoolToInt(UGlobalWork_HasMemberJoined(7)) + // Aigis
                BoolToInt(UGlobalWork_HasMemberJoined(8)) + // Ken
                BoolToInt(UGlobalWork_HasMemberJoined(9)) + // Koromaru
                BoolToInt(UGlobalWork_HasMemberJoined(6)); // Fuuka
            if (UGlobalWork_HasMemberJoined(10)) // Shinjiro
                return _common._getUGlobalWork()->GetBitflag(0x6f) 
                    ? foolBustupId + 1 : foolBustupId; // EVT_ARAGAKI_OUT
            return foolBustupId + 1;
        }
        private unsafe int UCmpCommu_GetPortraitTexIdHierophant(UCmpCommu* self)
        {
            var detailsMenuIdx = self->Actor_->CommuDetailsScene_->Menu->VisibleEntryOffset;
            return 10 + ((detailsMenuIdx == 1) ? 1 : 0);
        }
        // FUN_14129c4d0 (mostly)
        private unsafe int UCmpCommu_GetPortraitTexIdVanilla(UCmpCommu* self, int id)
        {
            return id switch
            {
                1 => UCmpCommu_GetPortraitTexIdFool(),
                2 => 6,
                3 => 7,
                4 => 8,
                5 => 9,
                6 => UCmpCommu_GetPortraitTexIdHierophant(self),
                7 => 0xc,
                8 => 0xd,
                9 => 0xe,
                10 => 0xf,
                0xb => 0x10,
                0xc => 0x11,
                0xd => 0x12,
                0xe => 0x13,
                0xf => 0x14,
                0x10 => 0x15,
                0x11 => 0x16,
                0x12 => 0x17,
                0x13 => 0x18,
                0x14 => 0x19,
                0x15 => 0x1a,
                0x16 => 0x1b,
                _ => 1,
            };
        }
        private unsafe UTexture2D* UCmpCommu_GetPortraitTexture(UCmpCommu* self)
        {
            var cmmMenu = self->Actor_->Menu;
            var currCmd = self->Actor_->UnlockedCmmEntries.Get<CmmPtr>(cmmMenu->VisibleEntryOffset + cmmMenu->ScrollEntryOffset)->ArcanaId;
            if (currCmd <= vanillaCmmLimit)
                return (&self->pCommuBustupAry)[UCmpCommu_GetPortraitTexIdVanilla(self, currCmd)];
            if (_manager.cmmIndexToSlHash.TryGetValue(currCmd, out var cmmHash) && _manager.activeSocialLinks.TryGetValue(cmmHash, out var customSl))
                return self->GetCustomSlPortrait(currCmd);
            return null;
        }

        private unsafe void UCmpCommu_DetailsDrawPortraitImpl(UCmpCommu* self, uint drawId)
        {
            BPDrawSpr* gDrawer = _common.GetDrawer();
            _common._setRenderTarget(gDrawer, 0, drawId);
            FSprColor commuBupTint = new FSprColor(0xff, 0xff, 0xff, 0xff);
            FVector commuBupPos = new FVector(1336, -297, 0);
            FVector2D commuBupTexcoord0 = new FVector2D(0, 0);
            FVector2D commuBupTexcoord1 = new FVector2D(1, 1);
            UTexture2D* commuBup = UCmpCommu_GetPortraitTexture(self);
            if (commuBup != null)
            {
                _drawTexture(&self->drawer, 
                    commuBupPos.X + commuBupPos.Y + self->Actor_->CommuDetailsScene_->CharDetailBgOffsetX,
                    57, 0, 2048, 2048, &commuBupTint, 1, 1, 0, &commuBupTexcoord0, &commuBupTexcoord1,
                    commuBup, 4, (int)drawId);
            }
            //var currCmd = self->Actor_->UnlockedCmmEntries.Get<CmmPtr>(self->MenuSelected);
            //_manager.UCommunityHandler_CmmCheckReverseImpl(currCmd);
            //_context._utils.Log($"[UCmpCommu::DetailsDrawPortraitImpl] GetPortraitTexture got {(nint):X}");
        }
    }
}
