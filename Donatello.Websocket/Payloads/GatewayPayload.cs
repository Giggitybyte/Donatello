﻿using System.Text.Json.Serialization;

namespace Donatello.Websocket.Payload
{
    public sealed class GatewayPayload
    {
		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName("op")]
		public int Opcode { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName("d")]
		public object Data { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName("s")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public int? Sequence { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[JsonPropertyName("t")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string EventName { get; set; }
	}
}
