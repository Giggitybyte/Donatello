using System;
using System.Text.Json;
using Donatello.Interactions.Entities;

namespace Donatello.Interactions.Extensions
{
    internal static class JsonElementExtensions
    {
        /// <summary>Deserializes the JSON token to a string array.</summary>
        internal static string[] ToStringArray(this JsonElement jsonArray)
        {
            if (jsonArray.ValueKind is not JsonValueKind.Array)
                throw new JsonException($"Expected Array, got {jsonArray.ValueKind} instead.");

            var index = 0;
            var array = new string[jsonArray.GetArrayLength()];

            foreach (var jsonElement in jsonArray.EnumerateArray())
            {
                if (jsonElement.ValueKind is not JsonValueKind.String)
                    throw new JsonException($"Expected a String element, got {jsonElement.ValueKind} element instead.");

                array[index++] = jsonElement.GetString();
            }

            return array;
        }

        /// <summary>Converts the JSON token to an array of Discord entities.</summary>
        internal static T[] ToEntityArray<T>(this JsonElement jsonArray) where T : DiscordEntity
        {
            if (jsonArray.ValueKind is not JsonValueKind.Array)
                throw new JsonException($"Expected Array, got {jsonArray.ValueKind} instead.");

            var index = 0;
            var array = new T[jsonArray.GetArrayLength()];

            foreach (var jsonElement in jsonArray.EnumerateArray())
                array[index++] = (T)Activator.CreateInstance(typeof(T), jsonElement);

            return array;
        }
    }
}
