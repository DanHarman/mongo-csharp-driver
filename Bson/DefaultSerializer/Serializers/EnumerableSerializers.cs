﻿/* Copyright 2010 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class EnumerableSerializer : BsonBaseSerializer {
        #region private static fields
        private static EnumerableSerializer singleton = new EnumerableSerializer();
        #endregion

        #region constructors
        private EnumerableSerializer() {
        }
        #endregion

        #region public static properties
        public static EnumerableSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ArrayList), singleton);
            BsonSerializer.RegisterSerializer(typeof(ICollection), singleton);
            BsonSerializer.RegisterSerializer(typeof(IEnumerable), singleton);
            BsonSerializer.RegisterSerializer(typeof(IList), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else if (bsonType == BsonType.Array) {
                bsonReader.ReadArrayName(out name);
                bsonReader.ReadStartDocument();
                var list = new ArrayList();
                while (bsonReader.HasElement()) {
                    var elementType = BsonClassMapSerializer.GetActualElementType(bsonReader, typeof(object));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    string elementName; // elementNames are ignored on input
                    var element = serializer.DeserializeElement(bsonReader, typeof(object), out elementName);
                    list.Add(element);
                }
                bsonReader.ReadEndDocument();
                return list;
            } else {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteArrayName(name);
                bsonWriter.WriteStartDocument();
                int index = 0;
                foreach (var element in (IEnumerable) value) {
                    var elementName = index.ToString();
                    BsonSerializer.SerializeElement(bsonWriter, typeof(object), elementName, element);
                    index++;
                }
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }

    public static class EnumerableSerializerRegistration {
        #region public static methods
        public static void RegisterGenericSerializerDefinitions() {
            BsonDefaultSerializer.RegisterGenericSerializerDefinition(typeof(HashSet<>), typeof(EnumerableSerializer<>));
            BsonDefaultSerializer.RegisterGenericSerializerDefinition(typeof(List<>), typeof(EnumerableSerializer<>));
            BsonDefaultSerializer.RegisterGenericSerializerDefinition(typeof(ICollection<>), typeof(EnumerableSerializer<>));
            BsonDefaultSerializer.RegisterGenericSerializerDefinition(typeof(IEnumerable<>), typeof(EnumerableSerializer<>));
            BsonDefaultSerializer.RegisterGenericSerializerDefinition(typeof(IList<>), typeof(EnumerableSerializer<>));
        }
        #endregion
    }

    public class EnumerableSerializer<T> : BsonBaseSerializer {
        #region constructors
        public EnumerableSerializer() {
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else if (bsonType == BsonType.Array) {
                bsonReader.ReadArrayName(out name);
                bsonReader.ReadStartDocument();
                var list = (nominalType.IsInterface) ? new List<T>() : (ICollection<T>) Activator.CreateInstance(nominalType);
                while (bsonReader.HasElement()) {
                    var elementType = BsonClassMapSerializer.GetActualElementType(bsonReader, typeof(T));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    string elementName; // elementNames are ignored on input
                    var element = (T) serializer.DeserializeElement(bsonReader, typeof(T), out elementName);
                    list.Add(element);
                }
                bsonReader.ReadEndDocument();
                return list;
            } else {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteArrayName(name);
                bsonWriter.WriteStartDocument();
                int index = 0;
                foreach (var element in (IEnumerable<T>) value) {
                    var elementName = index.ToString();
                    BsonSerializer.SerializeElement(bsonWriter, typeof(T), elementName, element);
                    index++;
                }
                bsonWriter.WriteEndDocument();
            }
        }
        #endregion
    }
}