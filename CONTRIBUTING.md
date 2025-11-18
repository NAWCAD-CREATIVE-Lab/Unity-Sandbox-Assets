# Contributing to This Project

**NOTE: This CONTRIBUTING.md is for software contributions. You do not need to follow the Developer's Certificate of Origin (DCO) process for commenting on this repository's documentation, such as CONTRIBUTING.md, INTENT.md, etc. or for submitting issues.**

Thanks for thinking about using or contributing to this software ("Project") and its documentation!

- [Submitting an Issue](#submitting-an-issue)
- [Policy & Legal Info](#policy)
- [Submitting Changes](#submitting-changes)

## Submitting an Issue

You should feel free to submit an issue to this repository for anything you find that needs attention in this package. That includes functionality, design, or anything else!

If submitting a bug report, please be sure to include accurate and thorough information about the problem you're observing. Be sure to include:
- Steps to reproduce the problem,
- The script or scene where you observed the problem,
- What you expected to happen,
- What actually happened (or didn't happen), and
- Technical details including your Unity version and runtime platform

## Policy

### 1. Introduction

The project maintainer for this Project will only accept contributions using the Developer's Certificate of Origin 1.1 located at [developercertificate.org](https://developercertificate.org) ("DCO"). The DCO is a legally binding statement asserting that you are the creator of your contribution, or that you otherwise have the authority to distribute the contribution, and that you are intentionally making the contribution available under the license associated with the Project ("License").

### 2. Developer Certificate of Origin Process

Before submitting contributing code to this repository for the first time, you'll need to sign a Developer Certificate of Origin (DCO) (see below). To agree to the DCO, add your name and email address to the [CONTRIBUTORS.md](CONTRIBUTORS.md) file. At a high level, adding your information to this file tells us that you have the right to submit the work you're contributing and indicates that you consent to our treating the contribution in a way consistent with the license associated with this software (as described in [LICENSE.md](LICENSE.md)) and its documentation ("Project").

### 3. Important Points

Pseudonymous or anonymous contributions are permissible, but you must be reachable at the email address provided in the Signed-off-by line.

If your contribution is significant, you are also welcome to add your name and copyright date to the source file header.

U.S. Federal law prevents the government from accepting gratuitous services unless certain conditions are met. By submitting a pull request, you acknowledge that your services are offered without expectation of payment and that you expressly waive any future pay claims against the U.S. Federal government related to your contribution.

If you are a U.S. Federal government employee and use a `*.mil` or `*.gov` email address, we interpret your Signed-off-by to mean that the contribution was created in whole or in part by you and that your contribution is not subject to copyright protections.

### 4. DCO Text

The full text of the DCO is included below and is available online at [developercertificate.org](https://developercertificate.org):

```txt
Developer Certificate of Origin
Version 1.1

Copyright (C) 2004, 2006 The Linux Foundation and its contributors.
1 Letterman Drive
Suite D4700
San Francisco, CA, 94129

Everyone is permitted to copy and distribute verbatim copies of this
license document, but changing it is not allowed.

Developer's Certificate of Origin 1.1

By making a contribution to this project, I certify that:

(a) The contribution was created in whole or in part by me and I
    have the right to submit it under the open source license
    indicated in the file; or

(b) The contribution is based upon previous work that, to the best
    of my knowledge, is covered under an appropriate open source
    license and I have the right under that license to submit that
    work with modifications, whether created in whole or in part
    by me, under the same open source license (unless I am
    permitted to submit under a different license), as indicated
    in the file; or

(c) The contribution was provided directly to me by some other
    person who certified (a), (b) or (c) and I have not modified
    it.

(d) I understand and agree that this project and the contribution
    are public and that a record of the contribution (including all
    personal information I submit with it, including my sign-off) is
    maintained indefinitely and may be redistributed consistent with
    this project or the open source license(s) involved.
```

## Submitting Changes

### 1. Getting Started

Clone this repository into the "Packages" folder of a new Unity project.

Rename the new folder created by git to the package's name declared at the top of [package.json](package.json).

### 2. Making Changes

Please read through [Unity's documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-layout.html) on their recommended package layout to ensure new content is added to the recommended folder.

New runtime scripts should be added to the [Runtime](Runtime/) folder, and declared under the CREATIVE.SandboxAssets namespace.

New editor scripts should be added to the [Editor](Editor/) folder, and declared under the CREATIVE.SandboxAssets.Editor namespace.

Each Sandbox Asset also has its own sub-folder and sub-namespace. Please see existing scripts for naming conventions. If adding a new type of Sandbox Asset, new subfolders and sub-namespaces should be similarly created.

To add or edit the samples included with this package, remove the ~ from the [Samples~](Samples~/) folder name. This will make the folder visible to Unity. When the Samples folder becomes visible, Unity will automatically create a Samples.meta file, as well as .meta files for each inner sample folder. Delete these .meta files and add the ~ back to the Samples folder name before committing any changes.

If adding a new script to this package (or a new feature to an existing script), create a new folder in the [Samples~](Samples~/) directory with one or more scenes that clearly demonstrate the new functionality. A new entry must also be added to the `samples` block in [package.json](package.json) for the new sample to be importable via Unity's package manager. Information on the formatting of [package.json](package.json) can be found in [Unity's documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-manifestPkg.html).

It is also helpful to add sample scenes that demonstrate where the new functionality is meant to error out.

Sample scenes should, ideally, only demonstrate the functionality of a single script, and not be dependent on other scripts in the package. If adding a new folder in [Samples~](Samples~/), prepend it with a number indicating its dependence on other scripts to function. Subfolders in [Samples~](Samples~/) prepended with 01 only require the scripts being sampled to function. Subfolders prepended with 02 obviously require the scripts being sampled, but also require one or more scripts sampled in a folder prepended with 01. It's easier to track down errors when the dependency graph is clear.

### 3. Code Style

Unless they are completely self-explanatory, document all public classes and class members in a manner [compatible with Doxygen](https://www.doxygen.nl/manual/docblocks.html#cppblock).

Adhere to Microsoft's .NET C# [identifier naming](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names) and [coding](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) conventions.

Avoid uneccessary public variables. Private variables can be accessed and edited from the inspector by adding the [SerializeField tag](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializeField.html).

Do not assume that public or serialized variables will remain constant at runtime. If a serialized field is referenced multiple times in the lifespan of a script and expected not to change, it should instead be copied to a secondary private variable before use.

The following pattern can be helpful in a MonoBehaviour:

```
    /*
        An example field that is serialized in the inspector

        It is referenced in the update callback, and could change at runtime,
        so it has a non-serialized private copy
    */
    [field: SerializeField] int           EditableField_1;
                            int registeredEditableField_1;

    [field: SerializeField] int           EditableField_2;
                            int registeredEditableField_2;

    // Indicates whether the serialized fields have been copied over 
    bool registered;

    void Start()       => reRegister();
    void OnEnable()    => reRegister();

#if UNITY_EDITOR
    void OnValidate()  => reRegister();
#endif

    void OnDisable()   => unRegister();
    void OnDestroy()   => unRegister();

    void reRegister()
    {
        unRegister();

        registeredEditableField_1 = EditableField_1;
        
        registeredEditableField_2 = EditableField_2;

        registered = true;
    }

    void unRegister()
    {
        if (registered)
        {
            // Whatever cleanup is necessary

            registered = false;
        }
    }

    void Update()
    {
        if (Application.isPlaying && isActiveAndEnabled && registered)
        {
            // Do things with registeredEditableField_1 and registeredEditableField_2
        }
    }
```

Your bug fix or feature addition won't be rejected if it runs afoul of any (or all) of these guidelines, but following the guidelines will definitely make everyone's lives a little easier.

### 4. Check Your Changes

Ensure that your new or modified functionality works as expected:
- In your sample scenes
- In a compiled executable
- When Game Objects with relevant Components are disabled
- When relevant Components are disabled but Game Objects remain enabled

### 5. Submitting A Merge Request

When making your changes, use a branch in git and submit a merge request.

After review by a maintainer, your request will either be commented on with a prompt for more information or changes, or it will be merged into the `main` branch.