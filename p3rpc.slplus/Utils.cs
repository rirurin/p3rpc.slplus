using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System.Runtime.InteropServices;

namespace p3rpc.slplus
{
    public class SocialLinkUtilities : ModuleBase<SocialLinkContext>
    {
        public unsafe SocialLinkUtilities(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules) {}
        public override void Register() { }
        public void Log(string text) => _context._utils.Log(text);
        public void LogError(string text) => _context._utils.Log(text, System.Drawing.Color.Red, LogLevel.Error);
        public string GetFName(FName name) => _context._objectMethods.GetFName(name);
        public FName GetFName(string name) => _context._objectMethods.GetFName(name);
        public unsafe TType* Malloc<TType>() where TType : unmanaged => _context._memoryMethods.FMemory_Malloc<TType>();
        public unsafe FString* MakeFString(string? text)
        {
            FString* newStr = _context._memoryMethods.FMemory_Malloc<FString>();
            if (text != null)
            {
                newStr->text.allocator_instance = (nint*)_context._memoryMethods.FMemory_Malloc(text.Length + 1, 8);
                nint marshallerToUtf16 = Marshal.StringToHGlobalUni($"{text}\0");
                NativeMemory.Copy((void*)marshallerToUtf16, newStr->text.allocator_instance, (nuint)(text.Length + 1) * 2);
                Marshal.FreeHGlobal(marshallerToUtf16);
            } else newStr->text.allocator_instance = null;
            newStr->text.arr_num = (text != null) ? text.Length + 1 : 0;
            newStr->text.arr_max = (text != null) ? text.Length + 1 : 0;
            return newStr;
        }
        public unsafe TArray<TArrayType>* MakeArray<TArrayType>(int entries) where TArrayType : unmanaged
        {
            var arr = _context._memoryMethods.FMemory_Malloc<TArray<TArrayType>>();
            arr->allocator_instance = _context._memoryMethods.FMemory_MallocMultiple<TArrayType>((uint)entries);
            arr->arr_num = entries;
            arr->arr_max = entries;
            return arr;
        }
    }
}
