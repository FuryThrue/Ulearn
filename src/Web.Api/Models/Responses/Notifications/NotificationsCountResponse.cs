﻿using System;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Notifications
{
	[DataContract]
	public class NotificationsCountResponse : ApiResponse
	{
		[DataMember(Name = "count")]
		public int Count { get; set; }

		[DataMember(Name = "last_timestamp")]
		public DateTime? LastTimestamp { get; set; }
	}
}