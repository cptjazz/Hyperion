﻿using System;
using System.IO;

namespace Wire.ValueSerializers
{
    public class ArraySerializer : ValueSerializer
    {
        public static readonly ArraySerializer Instance = new ArraySerializer();
        private readonly byte _manifest = 253;

        public override object ReadValue(Stream stream, SerializerSession session)
        {
            var elementSerializer = session.Serializer.GetSerializerByManifest(stream, session);
            //read the element type
            var elementType = elementSerializer.GetElementType();
            //get the element type serializer
            var length = stream.ReadInt32(session);
            var array = Array.CreateInstance(elementType, length); //create the array

            for (var i = 0; i < length; i++)
            {
                var s = session.Serializer.GetSerializerByManifest(stream, session);
                var value = s.ReadValue(stream, session); //read the element value
                array.SetValue(value, i); //set the element value
            }
            return array;
        }

        public override Type GetElementType()
        {
            throw new NotSupportedException();
        }

        public override void WriteManifest(Stream stream, Type type, SerializerSession session)
        {
            stream.WriteByte(_manifest);
        }

        public override void WriteValue(Stream stream, object value, SerializerSession session)
        {
            var array = value as Array;
            var elementType = value.GetType().GetElementType();
            var elementSerializer = session.Serializer.GetSerializerByType(elementType);
            elementSerializer.WriteManifest(stream, elementType, session); //write array element type
            stream.WriteInt32(array.Length);
            for (var i = 0; i < array.Length; i++) //write the elements
            {
                var elementValue = array.GetValue(i);
                if (elementValue == null)
                {
                    NullSerializer.Instance.WriteManifest(stream,null,session);
                }
                else
                {
                    var vType = elementValue.GetType();
                    var s2 = elementSerializer;
                    if (vType != elementType)
                    {
                        //value is of subtype, lookup the serializer for that type
                        s2 = session.Serializer.GetSerializerByType(vType);
                    }
                    //lookup serializer for subtype
                    s2.WriteManifest(stream, vType, session);
                    s2.WriteValue(stream, elementValue, session);
                }
            }
        }
    }
}