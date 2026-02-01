// <copyright file="ButtplugClientDeviceFeature.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    /// <summary>
    /// Represents a single feature of a Buttplug device.
    /// </summary>
    public class ButtplugClientDeviceFeature
    {
        /// <summary>
        /// The parent device.
        /// </summary>
        public ButtplugClientDevice Device { get; }

        /// <summary>
        /// The feature index within the device.
        /// </summary>
        public uint FeatureIndex { get; }

        /// <summary>
        /// Human-readable description of this feature.
        /// </summary>
        public string FeatureDescription { get; }

        /// <summary>
        /// The underlying feature definition.
        /// </summary>
        public DeviceFeature FeatureDefinition { get; }

        private readonly ButtplugClientMessageHandler _handler;

        internal ButtplugClientDeviceFeature(
            ButtplugClientDevice device,
            DeviceFeature feature,
            ButtplugClientMessageHandler handler)
        {
            Device = device;
            FeatureIndex = feature.FeatureIndex;
            FeatureDescription = feature.FeatureDescription;
            FeatureDefinition = feature;
            _handler = handler;
        }

        /// <summary>
        /// Checks if this feature has the specified output type.
        /// </summary>
        public bool HasOutput(OutputType outputType)
        {
            return FeatureDefinition.HasOutput(outputType);
        }

        /// <summary>
        /// Checks if this feature has the specified input type.
        /// </summary>
        public bool HasInput(InputType inputType)
        {
            return FeatureDefinition.HasInput(inputType);
        }

        /// <summary>
        /// Gets the output range for a specific output type.
        /// </summary>
        /// <param name="outputType">The output type.</param>
        /// <param name="min">The minimum value (output parameter).</param>
        /// <param name="max">The maximum value (output parameter).</param>
        /// <returns>True if the output type is supported and has a range, false otherwise.</returns>
        public bool TryGetOutputRange(OutputType outputType, out int min, out int max)
        {
            var output = FeatureDefinition.GetOutput(outputType);
            if (output?.Value != null && output.Value.Length >= 2)
            {
                min = output.Value[0];
                max = output.Value[1];
                return true;
            }
            min = 0;
            max = 0;
            return false;
        }

        /// <summary>
        /// Sends an output command to this feature.
        /// </summary>
        /// <param name="command">The output command to send.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task RunOutputAsync(DeviceOutputCommand command, CancellationToken token = default)
        {
            if (!HasOutput(command.OutputType))
            {
                var supportedOutputs = FeatureDefinition.Output?.Keys ?? Enumerable.Empty<string>();
                var supportedList = supportedOutputs.Any()
                    ? string.Join(", ", supportedOutputs)
                    : "none";
                throw new ButtplugDeviceException(
                    $"Feature {FeatureIndex} ({FeatureDescription}) on device '{Device.Name}' does not support output type '{command.OutputType}'. " +
                    $"Supported outputs: {supportedList}.");
            }

            // Get the output definition to determine step range
            var outputDef = FeatureDefinition.GetOutput(command.OutputType);
            int maxSteps = 100; // Default if not specified
            if (outputDef?.Value != null && outputDef.Value.Length >= 2)
            {
                maxSteps = outputDef.Value[1];
            }

            // Convert to actual step value
            int actualValue = command.Value.ToStepValue(maxSteps);

            OutputCmd cmd;
            if (command.OutputType == OutputType.HwPositionWithDuration)
            {
                if (!command.Duration.HasValue)
                {
                    throw new ButtplugDeviceException(
                        $"HwPositionWithDuration command for feature {FeatureIndex} ({FeatureDescription}) on device '{Device.Name}' requires a duration value. " +
                        "Use DeviceOutput.PositionWithDuration.Percent(position, durationMs) or DeviceOutput.PositionWithDuration.Steps(steps, durationMs).");
                }
                cmd = OutputCmd.CreatePositionWithDuration(Device.Index, FeatureIndex, actualValue, command.Duration.Value);
            }
            else
            {
                cmd = OutputCmd.Create(Device.Index, FeatureIndex, command.OutputType, actualValue);
            }

            await _handler.SendMessageExpectOk(cmd, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an input command to this feature.
        /// For Read commands, returns the reading. For Subscribe/Unsubscribe, returns null.
        /// </summary>
        /// <param name="command">The input command to send.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The input reading for Read commands, null for Subscribe/Unsubscribe.</returns>
        public async Task<InputReading> RunInputAsync(DeviceInputCommand command, CancellationToken token = default)
        {
            if (!HasInput(command.InputType))
            {
                var supportedInputs = FeatureDefinition.Input?.Keys ?? Enumerable.Empty<string>();
                var supportedList = supportedInputs.Any()
                    ? string.Join(", ", supportedInputs)
                    : "none";
                throw new ButtplugDeviceException(
                    $"Feature {FeatureIndex} ({FeatureDescription}) on device '{Device.Name}' does not support input type '{command.InputType}'. " +
                    $"Supported inputs: {supportedList}.");
            }

            var cmd = new InputCmd(Device.Index, FeatureIndex, command.InputType, command.CommandType);

            if (command.CommandType == InputCommandType.Read)
            {
                var result = await _handler.SendMessageAsync(cmd, token).ConfigureAwait(false);

                switch (result)
                {
                    case InputReading reading:
                        return reading;
                    case Error err:
                        throw ButtplugException.FromError(err);
                    default:
                        throw new ButtplugMessageException($"Message type {result.Name} not handled by RunInputAsync", result.Id);
                }
            }
            else
            {
                // Subscribe/Unsubscribe - expect Ok
                await _handler.SendMessageExpectOk(cmd, token).ConfigureAwait(false);
                return null;
            }
        }
    }
}
