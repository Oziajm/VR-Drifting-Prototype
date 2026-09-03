# VR Drifting Prototype

A work-in-progress VR driving prototype developed in Unity and C#.

The project explores immersive cockpit interaction, vehicle handling,
drift detection and score-based gameplay.

[![Watch the VR Drifting gameplay demo](https://img.youtube.com/vi/Xr9ut6IvLGg/maxresdefault.jpg)](https://www.youtube.com/watch?v=Xr9ut6IvLGg)

▶️ [Watch the gameplay demo on YouTube](https://www.youtube.com/watch?v=Xr9ut6IvLGg)

## Key Features

- Fully interactive VR cockpit with a physical steering wheel,
  handbrake and manual H-pattern gear shifter
- Five forward gears, neutral and reverse
- Rear-wheel-drive vehicle handling based on Unity WheelColliders
- Dynamic rear-wheel slip, grip recovery and counter-steering assistance
- Drift detection based on lateral velocity, vehicle speed and wheel slip
- Burnout detection with visual and audio feedback
- Drift scoring with multipliers, steering bonuses and a combo grace period
- RPM-based engine power and audio
- Race mode with countdown, ordered checkpoints, timer and progress tracking
- Configurable vehicle and scoring parameters using ScriptableObjects

## Technical Highlights

### Physical VR Controls

The cockpit controls are operated directly using VR controllers.
The steering wheel calculates its rotation from the position of the player's
hand, while the gear shifter recognizes individual gate positions for
five forward gears, neutral and reverse. The handbrake provides an
analog braking value based on its physical position.

### Vehicle and Drift System

The vehicle uses Unity WheelColliders with a custom gameplay layer for
rear-wheel slip, gear-dependent torque, grip recovery, drift assistance
and counter-steering. Drift state is determined using lateral velocity,
vehicle speed and rear-axle slip.

### Drift Scoring

Drift points accumulate over time and are affected by steering input
and configurable multiplier thresholds. A grace-period system allows
players to connect drifts, while significant collisions cancel the
current combo.

### Architecture

Vehicle input, driving physics, wheel handling, audiovisual effects,
cockpit presentation and scoring are separated into dedicated components.
Configuration data is stored in ScriptableObjects, while gameplay systems
communicate through events.

## Project Status

This is a work-in-progress technical prototype. It demonstrates the core
driving, VR interaction and scoring systems, but is not a finished game.

## Technologies

- Unity 2023
- C#
- OpenXR
- XR Interaction Toolkit
- Universal Render Pipeline
- Unity Input System
- WheelColliders
- ScriptableObjects

## Selected Code Samples

The repository contains selected components from the physical VR cockpit
system. They are presented for code review and do not constitute the
complete Unity project.

- [Steering wheel](CodeSamples/PhysicalVRCockpit/SteeringWheel.cs)  
  Converts the position of the player's hand into constrained and
  smoothed steering-wheel rotation.

- [Manual gear shifter](CodeSamples/PhysicalVRCockpit/GearShifter.cs)  
  Implements an H-pattern gearbox with gate detection, snapping,
  five forward gears, neutral and reverse.

- [Handbrake](CodeSamples/PhysicalVRCockpit/Handbrake.cs)  
  Converts the physical position of the VR handbrake into an analog
  braking value.
