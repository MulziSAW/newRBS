// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace Epsara {

	using System;
	using System.Runtime.InteropServices;

#region Autogenerated code
	public class Data {

		[DllImport("libepsara-0.dll")]
		static extern IntPtr epsara_data_get_row(IntPtr matrix, int i);

		public static Epsara.DataVector GetRow(Epsara.DataMatrix matrix, int i) {
			IntPtr raw_ret = epsara_data_get_row(matrix == null ? IntPtr.Zero : matrix.Handle, i);
			Epsara.DataVector ret = GLib.Object.GetObject(raw_ret) as Epsara.DataVector;
			return ret;
		}

		[DllImport("libepsara-0.dll")]
		static extern IntPtr epsara_data_get_column(IntPtr matrix, int j);

		public static Epsara.DataVector GetColumn(Epsara.DataMatrix matrix, int j) {
			IntPtr raw_ret = epsara_data_get_column(matrix == null ? IntPtr.Zero : matrix.Handle, j);
			Epsara.DataVector ret = GLib.Object.GetObject(raw_ret) as Epsara.DataVector;
			return ret;
		}

		[DllImport("libepsara-0.dll")]
		static extern IntPtr epsara_data_measruement_get_schedule(IntPtr measurement);

		public static GLib.SList MeasruementGetSchedule(Epsara.DataMeasurement measurement) {
			IntPtr raw_ret = epsara_data_measruement_get_schedule(measurement == null ? IntPtr.Zero : measurement.Handle);
			GLib.SList ret = new GLib.SList(raw_ret);
			return ret;
		}

#endregion
	}
}
