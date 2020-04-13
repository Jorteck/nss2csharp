﻿using System.CodeDom.Compiler;
using nss2csharp.Language;
using nss2csharp.Parser;
using System.Collections.Generic;
using System.IO;

namespace nss2csharp.Output
{
    class Output_CSharp : IOutput
    {
        private static CodeDomProvider CodeDomProvider = CodeDomProvider.CreateProvider("C#");

        public int GetFromTokens(IEnumerable<IToken> tokens, out string data)
        {
            data = null;
            return 1;
        }

        public int GetFromCU(CompilationUnit cu, out string data, out string className)
        {
            if (cu.m_Metadata.m_Name == "nwscript.nss")
            {
                Output_CSharp_NwscriptNss output = new Output_CSharp_NwscriptNss();
                className = "NWScript";
                return output.GetFromCU(cu, className, out data);
            }
            else if (cu.m_Metadata.m_Name.StartsWith("nwnx") && cu.m_Metadata.m_Name.EndsWith(".nss"))
            {
                Output_CSharp_NWNX output = new Output_CSharp_NWNX();

                className = Path.GetFileNameWithoutExtension(cu.m_Metadata.m_Name);
                className = className.Substring(className.LastIndexOf("_") + 1);
                className = char.ToUpper(className[0]) + className.Substring(1) + "Plugin";

                return output.GetFromCU(cu, className, out data);
            }

            data = null;
            className = null;
            return 1;
        }

        public static string GetTypeAsString(Type type, bool nullable = true)
        {
            if (type.GetType() == typeof(VoidType))              return "void";
            else if (type.GetType() == typeof(IntType))          return "int";
            else if (type.GetType() == typeof(FloatType))        return "float";
            else if (type.GetType() == typeof(StringType))       return "string";
            else if (type.GetType() == typeof(StructType))       return ((StructType)type).m_TypeName;
            else if (type.GetType() == typeof(ObjectType))       return "uint";
            else if (type.GetType() == typeof(LocationType))     return "NWN.Location";
            else if (type.GetType() == typeof(VectorType))       return "NWN.Vector";
            else if (type.GetType() == typeof(ItemPropertyType)) return "NWN.ItemProperty";
            else if (type.GetType() == typeof(EffectType))       return "NWN.Effect";
            else if (type.GetType() == typeof(TalentType))       return "NWN.Talent";
            else if (type.GetType() == typeof(EventType))        return "NWN.Event";
            else if (type.GetType() == typeof(ActionType))       return "NWN.ActionDelegate";

            return null;
        }

        public static string GetValueAsString(Value value, bool isPlugin)
        {
            const string floatFormatStr = "0.0#######";

            if (value is Lvalue lv)
            {
                if (lv.m_Identifier == "OBJECT_SELF")
                {
                    return isPlugin ? "NWN.NWScript.OBJECT_INVALID" : "OBJECT_INVALID";
                }

                if (isPlugin && !lv.m_Identifier.StartsWith("NWNX_"))
                {
                    return $"NWN.NWScript.{lv.m_Identifier}";
                }

                return GetSafeVariableName(lv.m_Identifier);
            }
            else if (value is IntLiteral intLit)
            {
                return intLit.m_Value.ToString();
            }
            else if (value is FloatLiteral floatLit)
            {
                return floatLit.m_Value.ToString(floatFormatStr) + "f";
            }
            else if (value is StringLiteral stringLit)
            {
                return stringLit.m_Value;
            }
            else if (value is VectorLiteral vectorLiteral)
            {
                return "null";
            }

            return null;
        }

        public static string GetStackPush(Type type, Value val, bool isPlugin)
        {
            if (type.GetType() == typeof(IntType)) return string.Format("NWN.Internal.NativeFunctions.StackPushInteger({0})", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(FloatType)) return string.Format("NWN.Internal.NativeFunctions.StackPushFloat({0})", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(StringType)) return string.Format("NWN.Internal.NativeFunctions.StackPushString({0})", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(ObjectType)) return string.Format("NWN.Internal.NativeFunctions.StackPushObject({0})", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(LocationType)) return string.Format("NWN.Internal.NativeFunctions.StackPushLocation({0}.Handle)", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(VectorType)) return string.Format("NWN.Internal.NativeFunctions.StackPushVector({0}.Handle)", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(ItemPropertyType)) return string.Format("NWN.Internal.NativeFunctions.StackPushItemProperty({0}.Handle)", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(EffectType)) return string.Format("NWN.Internal.NativeFunctions.StackPushEffect({0}.Handle)", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(TalentType)) return string.Format("NWN.Internal.NativeFunctions.StackPushTalent({0}.Handle)", GetValueAsString(val, isPlugin));
            else if (type.GetType() == typeof(EventType)) return string.Format("NWN.Internal.NativeFunctions.StackPushEvent({0}.Handle)", GetValueAsString(val, isPlugin));

            return null;
        }

        public static string GetStackPop(Type type)
        {
            if (type.GetType() == typeof(IntType)) return "NWN.Internal.NativeFunctions.StackPopInteger()";
            else if (type.GetType() == typeof(FloatType)) return "NWN.Internal.NativeFunctions.StackPopFloat()";
            else if (type.GetType() == typeof(StringType)) return "NWN.Internal.NativeFunctions.StackPopString()";
            else if (type.GetType() == typeof(ObjectType)) return "NWN.Internal.NativeFunctions.StackPopObject()";
            else if (type.GetType() == typeof(LocationType)) return "NWN.Internal.NativeFunctions.StackPopLocation()";
            else if (type.GetType() == typeof(VectorType)) return "NWN.Internal.NativeFunctions.StackPopVector()";
            else if (type.GetType() == typeof(ItemPropertyType)) return "new NWN.ItemProperty(NWN.Internal.NativeFunctions.StackPopItemProperty())";
            else if (type.GetType() == typeof(EffectType)) return "new NWN.Effect(NWN.Internal.NativeFunctions.StackPopEffect())";
            else if (type.GetType() == typeof(TalentType)) return "new NWN.Talent(NWN.Internal.NativeFunctions.StackPopTalent())";
            else if (type.GetType() == typeof(EventType)) return "new NWN.Event(NWN.Internal.NativeFunctions.StackPopEvent())";

            return null;
        }

        public static string GetInternalCall(int id)
        {
            return string.Format("NWN.Internal.NativeFunctions.CallBuiltIn({0})", id);
        }

        public static string GetNWNXSetFunction(string pluginNameVar, string methodName)
        {
            return $"NWN.Internal.NativeFunctions.nwnxSetFunction({pluginNameVar}, \"{methodName}\")";
        }

        public static string GetSafeVariableName(string variable)
        {
            return !CodeDomProvider.IsValidIdentifier(variable) ? $"@{variable}" : variable;
        }
    }
}
