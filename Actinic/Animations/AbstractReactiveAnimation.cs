//
//  AbstractReactiveAnimation.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 - 2016
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using FoxSoft.Utilities;

// Device configuration
using Actinic.Output;

// Rendering
using Actinic.Rendering;
using Actinic.Utilities;

namespace Actinic.Animations
{
	public abstract class AbstractReactiveAnimation:AbstractAnimation
	{

		#region Audio Intensity and Frequency

		/// <summary>
		/// Decimal from 0 to 1 representing intensity of the lower frequencies
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Low_Intensity {
			get;
			private set;
		}

		/// <summary>
		/// Decimal from 0 to 1 representing intensity of the middle frequencies
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Mid_Intensity {
			get;
			private set;
		}

		/// <summary>
		/// Decimal from 0 to 1 representing intensity of the higher frequencies
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_High_Intensity {
			get;
			private set;
		}


		/// <summary>
		/// Decimal from 0 to 1 representing the frequency distribution, higher number being higher frequency
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Frequency_Distribution_Percentage {
			get;
			private set;
		}

		/// <summary>
		/// Decimal from 0 to 1 representing the average frequency distribution, higher number being higher frequency
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Average_Frequency_Distribution_Percentage {
			get;
			private set;
		}

		/// <summary>
		/// Decimal from 0 to 1 representing the change in frequency distribution, higher number being higher frequency
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Delta_Frequency_Distribution_Percentage {
			get;
			private set;
		}


		/// <summary>
		/// Decimal from 0 to 1 representing the change in intensity
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Delta_Intensity {
			get;
			private set;
		}

		/// <summary>
		/// Decimal from 0 to 1 representing the average intensity level
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Average_Intensity {
			get;
			private set;
		}

		/// <summary>
		/// Decimal from 0 to 1 representing the current unsmoothed intensity level
		/// </summary>
		/// <value>Decimal from 0 to 1</value>
		public double Audio_Realtime_Intensity {
			get;
			private set;
		}

		#endregion

		#region Color Shifting

		/// <summary>
		/// Red component of the current shifting color
		/// </summary>
		/// <value>Number from 0 to 255</value>
		protected byte ColorShift_Red {
			get;
			private set;
		}

		/// <summary>
		/// Green component of the current shifting color
		/// </summary>
		/// <value>Number from 0 to 255</value>
		protected byte ColorShift_Green {
			get;
			private set;
		}

		/// <summary>
		/// Blue component of the current shifting color
		/// </summary>
		/// <value>Number from 0 to 255</value>
		protected byte ColorShift_Blue {
			get;
			private set;
		}

		protected enum ColorShift_Mode
		{
			ShiftingRed,
			ShiftingGreen,
			ShiftingBlue
		}

		/// <summary>
		/// Specifies which color is being ramped up or down
		/// </summary>
		/// <value>One of <see cref="ColorShift_Mode"/>.</value>
		protected ColorShift_Mode ColorShift_LastMode {
			get;
			private set;
		}

		#endregion


		/// <summary>
		/// If true, number of VU meters are cut in half to allow for mirroring of lights
		/// </summary>
		protected bool MirrorMeters = true;

		/// <summary>
		/// If true, smoothing amount automatically set based on audio intensity
		/// </summary>
		protected bool EnableAlgorithmicSmoothingControl = true;


		/// <summary>
		/// Collection representing the most-recent intensity-levels of sound
		/// </summary>
		/// <value>Current collection of sound intensity-levels</value>
		public List<double> AudioSnapshot {
			get;
			protected set;
		}

		/// <summary>
		/// Collection representing the most-recent intensity-levels of sound bundled together
		/// </summary>
		/// <value>Current collection of sound intensity-levels bundled for the light count</value>
		public List<double> AudioProcessedSnapshot {
			get;
			protected set;
		}


		public AbstractReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration) : base (Configuration)
		{
			// No need to enable RequestSmoothCrossfade as it's assumed no previous frame is available
			InitializeProperties ();
		}

		public AbstractReactiveAnimation (
			ReadOnlyDeviceConfiguration Configuration,
			Layer PreviouslyShownFrame)
			: base (Configuration, PreviouslyShownFrame)
		{
			RequestSmoothCrossfade = true;
			// By default, this will immediately override the existing colors.  Set to true to smoothly transition.
			//  If specific animations can make use of the previous frame, they can override this back to false.
			InitializeProperties ();
		}


		private void InitializeProperties ()
		{
			// Set up audio averaging
			audioSmoothingFilter = new ScaledAverage (deviceConfig);
			// Audio averaging is equivalent to 0.3 at 50 ms per frame
			// ms = ((2÷0.3)−1)×50
			// ms = 283.333333333
			audioSmoothingFilter.TimeConstant = 283;

			EnableSmoothing = true;
			// Most animations look better with smoothing applied.  Specific animations can turn this back off if needed

			AudioSnapshot = new List<double> ();
			AudioProcessedSnapshot = new List<double> ();
			// Need to make sure these aren't null

			Audio_Frequency_Distribution_Percentage = 0.5;
			Audio_Average_Frequency_Distribution_Percentage = 0.5;
			// Middle of range from 0 to 1, used for keeping track of frequency

			ColorShift_Red = LightSystem.Color_MAX;
			ColorShift_Green = LightSystem.Color_MIN;
			ColorShift_Blue = LightSystem.Color_MIN;
			ColorShift_LastMode = ColorShift_Mode.ShiftingRed;
			// Prepare the color-shifting system with a good set of defaults
		}


		#region Generic Processing

		public void UpdateAudioSnapshot (List<double> NewAudioSnapshot)
		{
			// Set up the current snapshot
			AudioSnapshot = new List<double> (NewAudioSnapshot);
			// Calculate intensity information
			UpdateIntensity (AudioSnapshot);
			// Calculate processed, bundled volume information
			UpdateVolumes (AudioSnapshot);
		}



		private void UpdateIntensity (List<double> Current_Audio_Volumes)
		{
			double bar_1 = 0;
			double bar_2 = 0;
			double bar_3 = 0;

			int i_volume_max = Current_Audio_Volumes.Count;
			double i_volume_percent = 0;
			for (int i_volume = 0; i_volume < i_volume_max; i_volume++) {
				i_volume_percent = ((double)i_volume / i_volume_max);
				if (i_volume_percent >= ReactiveSystem.Audio_Volume_Low_Percentage & i_volume_percent < ReactiveSystem.Audio_Volume_Mid_Percentage) {
					bar_1 = Math.Max (Current_Audio_Volumes [i_volume], bar_1);
				} else if (i_volume_percent >= ReactiveSystem.Audio_Volume_Mid_Percentage & i_volume_percent < ReactiveSystem.Audio_Volume_High_Percentage) {
					bar_2 = Math.Max (Current_Audio_Volumes [i_volume], bar_2);
				} else {
					bar_3 = Math.Max (Current_Audio_Volumes [i_volume], bar_3);
				}
			}

			UpdateIntensity (bar_1, bar_2, bar_3);
		}

		private void UpdateIntensity (double Audio_Volume_Low, double Audio_Volume_Mid, double Audio_Volume_High)
		{
			Audio_Low_Intensity = Audio_Volume_Low;
			Audio_Mid_Intensity = Audio_Volume_Mid;
			Audio_High_Intensity = Audio_Volume_High;

			Audio_Frequency_Distribution_Percentage = Math.Max (Math.Min ((((1 - Audio_Volume_Low) + (Audio_Volume_Mid * 0.3 + Audio_Volume_High * 0.6)) / 2), 1), 0);
			Audio_Delta_Frequency_Distribution_Percentage = (Audio_Frequency_Distribution_Percentage - Audio_Average_Frequency_Distribution_Percentage);
			Audio_Average_Frequency_Distribution_Percentage = audioSmoothingFilter.Filter (
				Audio_Average_Frequency_Distribution_Percentage,
				Audio_Frequency_Distribution_Percentage
			);

			Audio_Realtime_Intensity = Math.Max (Math.Min ((Audio_Volume_Low * 0.25 + Audio_Volume_Mid * 0.45 + Audio_Volume_High * 0.5), 1), 0);
			Audio_Delta_Intensity = (Audio_Realtime_Intensity - Audio_Average_Intensity);
			Audio_Average_Intensity = audioSmoothingFilter.Filter (
				Audio_Average_Intensity, Audio_Realtime_Intensity
			);

			// How fast or slowly the lights will change
			if (EnableAlgorithmicSmoothingControl) {
				// Convert from old FPS-dependent values to independent values
				double SmoothingAmount = Math.Min (Math.Max ((1 - (Audio_Average_Intensity)), 0.1), 0.75);
				SmoothingConstant = ((2 / (1 - SmoothingAmount)) - 1) * 50;
			}
			// ColorShift_Amount is deprecated, replaced by calculating on
			// demand in individual animations
			//ColorShift_Amount = Convert.ToByte (Math.Max (Math.Min ((Audio_Average_Intensity * 16) + 5, 255), 0));
		}

		private void UpdateVolumes (List<double> Current_Audio_Volumes)
		{
			lock (AudioProcessedSnapshot) {
				AudioProcessedSnapshot.Clear ();
				double NextStep = ReactiveSystem.Audio_Frequency_Scale_Start;
				double CurrentLight_Audio_Max = 0;

				if (ReactiveSystem.Audio_Frequency_Scale_Linear) {
					NextStep = 0;
				}
				if (ReactiveSystem.Audio_Frequency_Scale_Multiplier == 0 || ReactiveSystem.Audio_Frequency_Scale_Multiplier == 1) {
					ReactiveSystem.Processing_CalculateFrequencyStepMultiplier (Current_Audio_Volumes, MirrorMeters);
				}
				for (int i = 0; i < Current_Audio_Volumes.Count; i++) {
					if (i < NextStep) {
						CurrentLight_Audio_Max = Math.Min (Math.Max (Math.Max (CurrentLight_Audio_Max, Current_Audio_Volumes [i]), 0), 1);
					} else {
						AudioProcessedSnapshot.Add (CurrentLight_Audio_Max);
						CurrentLight_Audio_Max = Math.Min (Math.Max (Current_Audio_Volumes [i], 0), 1);
						if (ReactiveSystem.Audio_Frequency_Scale_Linear) {
							NextStep = NextStep + 1;
							if (NextStep > (LightSystem.LIGHT_COUNT / 2))
								break;
						} else {
							NextStep = NextStep * ReactiveSystem.Audio_Frequency_Scale_Multiplier;
						}
					}
				}
				AudioProcessedSnapshot.Add (CurrentLight_Audio_Max);
			}
		}

		#endregion

		#region Common Tools

		/// <summary>
		/// Rotates the hue of the color-shift colors based on the color-shift amount
		/// </summary>
		/// <param name="ColorShift_Amount">Amount to shift colors.</param>
		protected void AnimationUpdateColorShift (byte ColorShift_Amount)
		{
			switch (ColorShift_LastMode) {
			case ColorShift_Mode.ShiftingRed:
				ColorShift_Red = (byte)Math.Max (ColorShift_Red - ColorShift_Amount, LightSystem.Color_MIN);
				ColorShift_Green = (byte)Math.Min (ColorShift_Green + ColorShift_Amount, LightSystem.Color_MAX);
				if (ColorShift_Red == LightSystem.Color_MIN & ColorShift_Green == LightSystem.Color_MAX) {
					ColorShift_LastMode = ColorShift_Mode.ShiftingGreen;
				}
				break;
			case ColorShift_Mode.ShiftingGreen:
				ColorShift_Green = (byte)Math.Max (ColorShift_Green - ColorShift_Amount, LightSystem.Color_MIN);
				ColorShift_Blue = (byte)Math.Min (ColorShift_Blue + ColorShift_Amount, LightSystem.Color_MAX);
				if (ColorShift_Green == LightSystem.Color_MIN & ColorShift_Blue == LightSystem.Color_MAX) {
					ColorShift_LastMode = ColorShift_Mode.ShiftingBlue;
				}
				break;
			case ColorShift_Mode.ShiftingBlue:
				ColorShift_Blue = (byte)Math.Max (ColorShift_Blue - ColorShift_Amount, LightSystem.Color_MIN);
				ColorShift_Red = (byte)Math.Min (ColorShift_Red + ColorShift_Amount, LightSystem.Color_MAX);
				if (ColorShift_Blue == LightSystem.Color_MIN & ColorShift_Red == LightSystem.Color_MAX) {
					ColorShift_LastMode = ColorShift_Mode.ShiftingRed;
				}
				break;
			default:
				throw new NotSupportedException (string.Format (
					"Unsupported ColorShift_Mode {0}",
					ColorShift_LastMode
				));
			}
		}

		#endregion

		#region Internal

		/// <summary>
		/// The device-adjusted smoothing filter for audio averaging
		/// </summary>
		private ScaledAverage audioSmoothingFilter;

		#endregion
	}
}

