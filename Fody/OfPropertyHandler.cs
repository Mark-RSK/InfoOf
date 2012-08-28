using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{


    void HandleOfPropertyGet(Instruction instruction, ILProcessor ilProcessor)
    {
        HandleOfProperty(instruction, ilProcessor, x => x.GetMethod);
    }

    void HandleOfPropertySet(Instruction instruction, ILProcessor ilProcessor)
    {
        HandleOfProperty(instruction, ilProcessor, x => x.SetMethod);
    }

    void HandleOfProperty(Instruction instruction, ILProcessor ilProcessor, Func<PropertyDefinition, MethodDefinition> func)
    {

        var properyNameInstruction = instruction.Previous;
        var propertyName = GetLdString(properyNameInstruction);

        var typeNameInstruction = properyNameInstruction.Previous;
        var typeName = GetLdString(typeNameInstruction);

        var assemblyNameInstruction = typeNameInstruction.Previous;
        var assemblyName = GetLdString(assemblyNameInstruction);

        var typeDefinition = GetTypeDefinition(assemblyName, typeName);

        var property = typeDefinition.Properties.FirstOrDefault(x => x.Name == propertyName);

        if (property == null)
        {
            throw new WeavingException(string.Format("Could not find property named '{0}'.", propertyName))
            {
                SequencePoint = instruction.SequencePoint
            };
        }
        var methodDefinition = func(property);
        if (methodDefinition == null)
        {
            throw new WeavingException(string.Format("Could not find property named '{0}'.", propertyName))
            {
                SequencePoint = instruction.SequencePoint
            };
        }
        ilProcessor.Remove(typeNameInstruction);
        ilProcessor.Remove(properyNameInstruction);
        ilProcessor.Replace(assemblyNameInstruction, Instruction.Create(OpCodes.Ldtoken, methodDefinition));
        if (typeDefinition.HasGenericParameters)
        {
            var typeReference = ModuleDefinition.Import(typeDefinition);
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldtoken, typeReference));
            instruction.Operand = getMethodFromHandleGeneric;
        }
        else
        {
            instruction.Operand = getMethodFromHandle;
        }

        ilProcessor.InsertAfter(instruction, Instruction.Create(OpCodes.Castclass, methodInfoType));
    }


}