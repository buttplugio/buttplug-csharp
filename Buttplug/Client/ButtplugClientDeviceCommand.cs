// <copyright file="ButtplugClientDeviceCommand.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    /// <summary>
    /// Represents either a percentage or a step value for device commands.
    /// </summary>
    public class PercentOrSteps
    {
        private readonly double? _percent;
        private readonly int? _steps;

        private PercentOrSteps(double? percent, int? steps)
        {
            _percent = percent;
            _steps = steps;
        }

        /// <summary>
        /// Gets the percentage value, if this represents a percentage.
        /// </summary>
        public double? Percent => _percent;

        /// <summary>
        /// Gets the step value, if this represents steps.
        /// </summary>
        public int? Steps => _steps;

        /// <summary>
        /// Creates a PercentOrSteps from a step value.
        /// </summary>
        /// <param name="steps">The step value.</param>
        /// <returns>A PercentOrSteps representing the step value.</returns>
        public static PercentOrSteps FromSteps(int steps)
        {
            return new PercentOrSteps(null, steps);
        }

        /// <summary>
        /// Creates a PercentOrSteps from a percentage value.
        /// </summary>
        /// <param name="percent">The percentage value.</param>
        /// <returns>A PercentOrSteps representing the percentage.</returns>
        /// <exception cref="ButtplugDeviceException">Thrown if percent is not in range.</exception>
        public static PercentOrSteps FromPercent(double percent, double minPercent = 0.0, double maxPercent = 1.0)
        {
            if (percent < minPercent || percent > maxPercent)
            {
                throw new ButtplugDeviceException($"Percent value {percent} is not in the range {minPercent} <= x <= {maxPercent}");
            }
            return new PercentOrSteps(percent, null);
        }

        /// <summary>
        /// Converts this value to an actual step value given a max step count.
        /// </summary>
        /// <param name="minSteps">The maximum number of steps in the negative direction.</param>
        /// <param name="maxSteps">The maximum number of steps.</param>
        /// <returns>The calculated step value.</returns>
        public int ToStepValue(int minSteps, int maxSteps)
        {
            if (_steps.HasValue)
            {
                return _steps.Value;
            }

            if (!_percent.HasValue)
            {
                return 0;
            }
            
            if (_percent.Value < 0)
            {
                return (int)Math.Floor(_percent.Value * minSteps * -1);
            }
            return (int)Math.Ceiling(_percent.Value * maxSteps);
        }
    }

    /// <summary>
    /// Represents an output command to be sent to a device feature.
    /// </summary>
    public class DeviceOutputCommand
    {
        /// <summary>
        /// The type of output (Vibrate, Rotate, Oscillate, etc.).
        /// </summary>
        public OutputType OutputType { get; }

        /// <summary>
        /// The value to set.
        /// </summary>
        public PercentOrSteps Value { get; }

        /// <summary>
        /// The duration in milliseconds (only for PositionWithDuration).
        /// </summary>
        public uint? Duration { get; }

        /// <summary>
        /// Creates a new output command.
        /// </summary>
        /// <param name="outputType">The output type.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="duration">Optional duration for PositionWithDuration.</param>
        public DeviceOutputCommand(OutputType outputType, PercentOrSteps value, uint? duration = null)
        {
            OutputType = outputType;
            Value = value;
            Duration = duration;
        }
    }

    /// <summary>
    /// Builder for creating output commands with a specific output type.
    /// </summary>
    public class DeviceOutputValueBuilder
    {
        private readonly OutputType _outputType;

        internal DeviceOutputValueBuilder(OutputType outputType)
        {
            _outputType = outputType;
        }

        /// <summary>
        /// Creates a command with the specified step value.
        /// </summary>
        /// <param name="steps">The step value.</param>
        /// <returns>The output command.</returns>
        public DeviceOutputCommand Steps(int steps)
        {
            return new DeviceOutputCommand(_outputType, PercentOrSteps.FromSteps(steps));
        }

        /// <summary>
        /// Creates a command with the specified percentage value.
        /// </summary>
        /// <param name="percent">The percentage.</param>
        /// <returns>The output command.</returns>
        public DeviceOutputCommand Percent(double percent)
        {
            var minPercent = _outputType == OutputType.Rotate ? -1.0 : 0.0;
            return new DeviceOutputCommand(_outputType, PercentOrSteps.FromPercent(percent, minPercent));
        }
    }

    /// <summary>
    /// Builder for creating PositionWithDuration output commands.
    /// </summary>
    public class DeviceOutputPositionWithDurationBuilder
    {
        internal DeviceOutputPositionWithDurationBuilder()
        {
        }

        /// <summary>
        /// Creates a position command with the specified step value and duration.
        /// </summary>
        /// <param name="steps">The position step value.</param>
        /// <param name="durationMs">The duration in milliseconds.</param>
        /// <returns>The output command.</returns>
        public DeviceOutputCommand Steps(int steps, uint durationMs)
        {
            return new DeviceOutputCommand(OutputType.HwPositionWithDuration, PercentOrSteps.FromSteps(steps), durationMs);
        }

        /// <summary>
        /// Creates a position command with the specified percentage value and duration.
        /// </summary>
        /// <param name="percent">The position percentage (0.0 to 1.0).</param>
        /// <param name="durationMs">The duration in milliseconds.</param>
        /// <returns>The output command.</returns>
        public DeviceOutputCommand Percent(double percent, uint durationMs)
        {
            return new DeviceOutputCommand(OutputType.HwPositionWithDuration, PercentOrSteps.FromPercent(percent), durationMs);
        }
    }

    /// <summary>
    /// Static class providing builders for device output commands.
    /// </summary>
    /// <example>
    /// <code>
    /// // Vibrate at 50%
    /// await device.RunOutputAsync(DeviceOutput.Vibrate.Percent(0.5));
    ///
    /// // Move to position 100 over 500ms
    /// await device.RunOutputAsync(DeviceOutput.PositionWithDuration.Percent(1.0, 500));
    /// </code>
    /// </example>
    public static class DeviceOutput
    {
        /// <summary>
        /// Builder for Vibrate commands.
        /// </summary>
        public static DeviceOutputValueBuilder Vibrate => new DeviceOutputValueBuilder(OutputType.Vibrate);

        /// <summary>
        /// Builder for Rotate commands.
        /// </summary>
        public static DeviceOutputValueBuilder Rotate => new DeviceOutputValueBuilder(OutputType.Rotate);

        /// <summary>
        /// Builder for Oscillate commands.
        /// </summary>
        public static DeviceOutputValueBuilder Oscillate => new DeviceOutputValueBuilder(OutputType.Oscillate);

        /// <summary>
        /// Builder for Constrict commands.
        /// </summary>
        public static DeviceOutputValueBuilder Constrict => new DeviceOutputValueBuilder(OutputType.Constrict);

        /// <summary>
        /// Builder for Temperature commands.
        /// </summary>
        public static DeviceOutputValueBuilder Temperature => new DeviceOutputValueBuilder(OutputType.Temperature);

        /// <summary>
        /// Builder for Led commands.
        /// </summary>
        public static DeviceOutputValueBuilder Led => new DeviceOutputValueBuilder(OutputType.Led);

        /// <summary>
        /// Builder for Spray commands.
        /// </summary>
        public static DeviceOutputValueBuilder Spray => new DeviceOutputValueBuilder(OutputType.Spray);

        /// <summary>
        /// Builder for Position commands (without duration).
        /// </summary>
        public static DeviceOutputValueBuilder Position => new DeviceOutputValueBuilder(OutputType.Position);

        /// <summary>
        /// Builder for PositionWithDuration commands.
        /// </summary>
        public static DeviceOutputPositionWithDurationBuilder PositionWithDuration => new DeviceOutputPositionWithDurationBuilder();
    }

    /// <summary>
    /// Represents an input command to be sent to a device feature.
    /// </summary>
    public class DeviceInputCommand
    {
        /// <summary>
        /// The type of input (Battery, RSSI, Button, etc.).
        /// </summary>
        public InputType InputType { get; }

        /// <summary>
        /// The command type (Read, Subscribe, Unsubscribe).
        /// </summary>
        public InputCommandType CommandType { get; }

        /// <summary>
        /// Creates a new input command.
        /// </summary>
        /// <param name="inputType">The input type.</param>
        /// <param name="commandType">The command type.</param>
        public DeviceInputCommand(InputType inputType, InputCommandType commandType)
        {
            InputType = inputType;
            CommandType = commandType;
        }
    }

    /// <summary>
    /// Builder for creating input commands with a specific input type.
    /// </summary>
    public class DeviceInputBuilder
    {
        private readonly InputType _inputType;

        internal DeviceInputBuilder(InputType inputType)
        {
            _inputType = inputType;
        }

        /// <summary>
        /// Creates a Read command.
        /// </summary>
        /// <returns>The input command.</returns>
        public DeviceInputCommand Read()
        {
            return new DeviceInputCommand(_inputType, InputCommandType.Read);
        }

        /// <summary>
        /// Creates a Subscribe command.
        /// </summary>
        /// <returns>The input command.</returns>
        public DeviceInputCommand Subscribe()
        {
            return new DeviceInputCommand(_inputType, InputCommandType.Subscribe);
        }

        /// <summary>
        /// Creates an Unsubscribe command.
        /// </summary>
        /// <returns>The input command.</returns>
        public DeviceInputCommand Unsubscribe()
        {
            return new DeviceInputCommand(_inputType, InputCommandType.Unsubscribe);
        }
    }

    /// <summary>
    /// Static class providing builders for device input commands.
    /// </summary>
    /// <example>
    /// <code>
    /// // Read battery level
    /// var reading = await device.RunInputAsync(DeviceInput.Battery.Read());
    ///
    /// // Subscribe to button events
    /// await device.RunInputAsync(DeviceInput.Button.Subscribe());
    /// </code>
    /// </example>
    public static class DeviceInput
    {
        /// <summary>
        /// Builder for Battery commands.
        /// </summary>
        public static DeviceInputBuilder Battery => new DeviceInputBuilder(InputType.Battery);

        /// <summary>
        /// Builder for RSSI commands.
        /// </summary>
        public static DeviceInputBuilder RSSI => new DeviceInputBuilder(InputType.RSSI);

        /// <summary>
        /// Builder for Button commands.
        /// </summary>
        public static DeviceInputBuilder Button => new DeviceInputBuilder(InputType.Button);

        /// <summary>
        /// Builder for Pressure commands.
        /// </summary>
        public static DeviceInputBuilder Pressure => new DeviceInputBuilder(InputType.Pressure);

        /// <summary>
        /// Builder for Depth commands.
        /// </summary>
        public static DeviceInputBuilder Depth => new DeviceInputBuilder(InputType.Depth);

        /// <summary>
        /// Builder for Position commands.
        /// </summary>
        public static DeviceInputBuilder Position => new DeviceInputBuilder(InputType.Position);
    }
}
