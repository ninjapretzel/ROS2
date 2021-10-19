using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class DateTimeUtils {


	private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long UnixTimestamp(this DateTime date) {
		TimeSpan diff = date.ToUniversalTime().Subtract(epoch);
		return (long)diff.TotalMilliseconds;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DateTime DateTimeFromUnixTimestamp(this long ms) {
		return epoch.AddMilliseconds(ms);
	}
}
