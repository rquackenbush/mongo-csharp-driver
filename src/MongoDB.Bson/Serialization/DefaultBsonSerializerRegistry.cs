﻿/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections.Concurrent;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Default, global implementation of an <see cref="IBsonSerializerRegistry"/>.
    /// </summary>
    public sealed class DefaultBsonSerializerRegistry : BsonSerializationProviderBase, IBsonSerializerRegistry
    {
        // private static fields
        private static readonly DefaultBsonSerializerRegistry __instance = new DefaultBsonSerializerRegistry();

        // private fields
        private readonly ConcurrentDictionary<Type, IBsonSerializer> _cache;
        private readonly ConcurrentDictionary<Type, Type> _serializerDefinitions;
        private readonly ConcurrentStack<IBsonSerializationProvider> _serializationProviders;

        // public static properties
        /// <summary>
        /// Gets the instance of the global registry.
        /// </summary>
        public static DefaultBsonSerializerRegistry Instance
        {
            get { return __instance; }
        }

        // constructors
        private DefaultBsonSerializerRegistry()
        {
            _cache = new ConcurrentDictionary<Type,IBsonSerializer>();
            _serializerDefinitions = new ConcurrentDictionary<Type, Type>();
            _serializationProviders = new ConcurrentStack<IBsonSerializationProvider>();

            // order matters. It's in reverse order of how they'll get consumed
            _serializationProviders.Push(new BsonClassMapSerializationProvider());
            _serializationProviders.Push(new DiscriminatedInterfaceSerializationProvider());
            _serializationProviders.Push(new CollectionsSerializationProvider());
            _serializationProviders.Push(new PrimitiveSerializationProvider());
            _serializationProviders.Push(new AttributedSerializationProvider());
            _serializationProviders.Push(new BsonObjectModelSerializationProvider());
        }

        // public methods
        /// <summary>
        /// Gets the serializer for the specified <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The serializer.
        /// </returns>
        public override IBsonSerializer GetSerializer(Type type)
        {
            return _cache.GetOrAdd(type, LookupSerializer);
        }

        /// <summary>
        /// Gets the serializer for the specified <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// The serializer.
        /// </returns>
        public IBsonSerializer<T> GetSerializer<T>()
        {
            return (IBsonSerializer<T>)GetSerializer(typeof(T));
        }

        /// <summary>
        /// Registers the serializer.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializer">The serializer.</param>
        public void RegisterSerializer(Type type, IBsonSerializer serializer)
        {
            if (typeof(BsonValue).IsAssignableFrom(type))
            {
                var message = string.Format("A serializer cannot be registered for type {0} because it is a subclass of BsonValue.", BsonUtils.GetFriendlyTypeName(type));
                throw new BsonSerializationException(message);
            }

            if (!_cache.TryAdd(type, serializer))
            {
                var message = string.Format("There is already a serializer registered for type {0}.", type.FullName);
                throw new BsonSerializationException(message);
            }
        }

        /// <summary>
        /// Registers the serializer definition.
        /// </summary>
        /// <param name="typeDefinition">The type.</param>
        /// <param name="serializerTypeDefinition">Type of the serializer.</param>
        public void RegisterSerializerDefinition(Type typeDefinition, Type serializerTypeDefinition)
        {
            // We are going to let last one win here. If the definition has
            // already produced a serializer, that's ok because that serializer
            // won't ever get regenerated again.
            if (!_serializerDefinitions.TryAdd(typeDefinition, serializerTypeDefinition))
            {
                var message = string.Format("There is already a serializer definition registered for type {0}.", typeDefinition.FullName);
                throw new BsonSerializationException(message);
            }
        }

        /// <summary>
        /// Registers the serialization provider. This behaves like a stack, so the 
        /// last provider registered is the first provider consulted.
        /// </summary>
        /// <param name="serializationProvider">The serialization provider.</param>
        public void RegisterSerializationProvider(IBsonSerializationProvider serializationProvider)
        {
            _serializationProviders.Push(serializationProvider);
        }

        // private methods
        private IBsonSerializer LookupSerializer(Type type)
        {
            Type serializerType;
            if (_serializerDefinitions.TryGetValue(type, out serializerType))
            {
                return CreateSerializer(serializerType);
            }
            else if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (_serializerDefinitions.TryGetValue(genericTypeDefinition, out serializerType))
                {
                    return CreateGenericSerializer(genericTypeDefinition, type.GetGenericArguments());
                }
            }

            foreach (var serializationProvider in _serializationProviders)
            {
                var serializer = serializationProvider.GetSerializer(type);
                if (serializer != null)
                {
                    return serializer;
                }
            }

            var message = string.Format("No serializer found for type {0}.", type.FullName);
            throw new BsonSerializationException(message);
        }
    }
}