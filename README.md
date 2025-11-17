# Sandbox Assets for the Unity Game Engine
This package contains a set of components, objects, and prefabs for the [Unity game engine](https://unity.com/) that facilitate a low-code, modular, event-driven style of development. It was originally authored by the CREATIVE Lab at [NAWCAD Lakehurst](https://www.navair.navy.mil/lakehurst/) for the purposes of training simulation development.

## Using in a project
The current version of this package is intended to be used with the most recent LTS version of Unity 6.

To use this package in a project, open the Packages folder, edit the manifest.json file, and add a new line to the `dependencies` block with the package's name (declared at the top of [package.json](package.json)) and this git repository's URL. More information on this specific feature of Unity can be found [in their documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html).

## The Philosophy of this Package
Inspiration for the development of the assets in this package came from two different talks about the capabilities of Scriptable Objects in Unity:
- [A 2016 talk by Richard Fine, of Unity Technologies](https://www.youtube.com/watch?v=6vmRwLYWNRo)
- [A 2017 talk by Ryan Hipple, of Schell Games](https://www.youtube.com/watch?v=raQ3iHhE_Kk)

Helpful development principles distilled from these talks include:
- State that is stored as variables in MonoBehaviours can be unintuitive to examine and modify
- MonoBehaviours receive a multitude of callbacks that may not be necessary for much of state management
- Scriptable Objects provide a useful way to store data in the Asset folder, outside of the Scene and MonoBehaviours
- Development is easier when architecture is modular, editable, and debuggable
- Singletons (or any kind of global managers) make debugging difficult
- Scripts should have a single clearly-defined responsibility
- Objects should be editable from the inspector
- Prefabs should be functional on their own, without them having to retrieve other dependencies
- The Unity Editor can be used as a tool to assign dependencies
- Systems should be modifiable without changing code
- Systems should be separated by layers of abstraction

This package provides a critical toolset for supporting development within these principles, in the form of a small set of basic data types. These data types are called "Sandbox Assets" because they live in the Asset folder and allow designers to craft functionality with more of a "sandbox" workflow than would normally be facilitated with traditional scripting. Once a collection of Sandbox Asset objects are instantiated and organized in the Asset folder, they can be referenced and used by scene components in a modular fashion. All of these Sandbox Assets can be accessed from the "Create" menu like every other Asset. This package also contains a multitude of sample scenes demonstrating ways the Sandbox Assets can be used. Information is given below about the breadth of scripts in the package, but the best way to start exploring is by examining the samples.

## Types of Sandbox Assets

### Events

The type of Sandbox Asset with the most capabilities in this package is the Sandbox Event. These are used in a way that is completely analogous to conventional event-driven programming, with components that invoke them and components that listen for them. Event-driven programming creates a helpful separation between causes and effects that keeps behavior de-coupled and modular.

All of the invokers and listeners in this package are configured visually in the Unity Editor, making complex systems of behavior quick to set up and easy to organize in a scene hierarchy. Sandbox Event objects themselves can be organized in the Asset folder, making it easy to get a sense at a glance for the types of behavior an Asset might exhibit.

After a scene starts, selecting a Sandbox Event will also show a custom inspector with lists of all objects that are either listening for the event, or may invoke it. This is very useful for debugging when it becomes difficult to tell when and where certain behavior is being triggered.

Components that can invoke Sandbox Events, triggered by standard behavior of the Unity Engine include:
- [Scene Start Invoker](Runtime/Events/Scripts/SceneStartInvoker.cs)
- [Button Invoker](Runtime/Events/Scripts/ButtonInvoker.cs)
- [Collider Invoker](Runtime/Events/Scripts/ColliderInvoker.cs)
- [Input Action Invoker](Runtime/Events/Scripts/InputActionInvoker.cs)
- [Animation State Invoker](Runtime/Events/Scripts/AnimationStateInvoker.cs)
- [Look-Interact Invoker](Runtime/Events/Scripts/LookInteractInvoker.cs)

To listen for a Sandbox Event and perform operations when it happens, add a [Scene Listener](Runtime/Events/Scripts/SceneListener.cs) to any object in a scene. It exposes a [Unity Event](https://docs.unity3d.com/6000.0/Documentation/Manual/unity-events.html) that can be used to configure any manner of behavior that should occur whenever a particular Event is invoked.

An [Asset Listener](Runtime/Events/Scripts/AssetListener.cs) can also be used in the same way, except it is instantiated in the Asset folder instead of the scene. This would be used to configure operations that should occur regardless of what scene might currently be playing.

Three Event objects are provided by default in this package, as they are the most commonly used (although optional): 
- [Scene Started](Runtime/Events/Scene Started.asset)
- [User Interacted](Runtime/Events/User Interacted.asset)
- [User Looked](Runtime/Events/User Looked.asset)

### Behavior Trees

Behavior Trees are essentially state machines created in the in the Asset folder that interact with other Sandbox Assets. These operate in a very similar way to Unity's standard Animator, but without the overhead of actual animations. The trees are viewable in a custom editor panel accessible from the "Window" dropdown. From this Behavior Tree Editor window (with a Behavior Tree asset selected) nodes can be added, connected, and arranged freely on an endless background. Once a Behavior Tree has been created, it can be run in a scene by referencing it from a [Behavior Tree Runner](Runtime/Behavior Trees/BehaviorTreeRunner.cs) component.

The types of nodes that can be added to a Behavior Tree include:
- [Listener Node](Runtime/Behavior Trees/Event Nodes/Listener Node/ListenerNode.cs)
- [Invoker Node](Runtime/Behavior Trees/Event Nodes/Invoker Node/InvokerNode.cs)

### Items

A Sandbox Item is a way to bind together information about a usable item in a scene. Several different types of inventory systems can be constructed through Sandbox Items without having to write any new code. Each Item contains references for a three-dimensional Prefab, a two-dimensional icon, and an optional Sandbox Event that would be invoked when the item is "used" in the playback of the scene. These Sandbox Items can then be referenced in the scene and passed between locations.

Items are "contained" in Game Objects that have certain components attached to them. These containers could be anything within the context of a scene: an inventory slot, an equip slot, a place in the environment to pick up or drop an item, etc. These Game Objects communicate with each other by way of Sandbox Events in order to move items around to different containers.

Sandbox Item containers have 3 different binary properties, making 8 different types of containers. These containers can be either:
- 2D or 3D, depending on how the Item should be represented
- Primary or Secondary, depending on whether the container is the default one for the scene
- A container or a container group, depending on whether the object can hold one or multiple items

A scene may have both a Primary Container and a Primary Container Group (a default equip slot and a default inventory, for instance), but a scene may not have multiple Primary Containers or multiple Primary Container Groups.

All 8 of these container types have been provided as [prefabs in this package](Runtime/Items/Prefabs/). But they can be all be constructed and used manually with the following components:
- [Item Reference](Runtime/Items/Scripts/ItemReference.cs)
- [Item Container](Runtime/Items/Scripts/ItemContainer.cs)
- [Item Container Group](Runtime/Items/Scripts/ItemContainerGroup.cs)

The default Item container prefabs also use some [default Sandbox Events](Runtime/Items/Events/) to communicate and move Items between each other.

## More Info
More specific information on each utility can be found in the comments of the C# files, which can also be used to generate HTML documentation with [Doxygen](https://www.doxygen.nl/)

For information on the licensing of this package, please see [INTENT.md](INTENT.md).

For information on how to contribute to this package please see [CONTRIBUTING.md](CONTRIBUTING.md).