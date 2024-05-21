# Interactive Code Execution

This project contains code relating to my ISA (Individual Student Activity) *"Interactive Code Execution in ScalableTeaching"*. 

The project runs from February 1st to June 30th.

Student: **Alexander Nørup**, alnoe20@student.sdu.dk

Supervisors:
- **Aisha Umair**: aiu@mmmi.sdu.dk
- **Miguel Enrique Campusano Araya**: mica@mmmi.sdu.dk

## Running the code

In order to clone the repository, you must also clone the submodules. This can be done using:

```bash
git clone --recurse-submodules https://github.com/AlexanderNorup/InteractiveCodeExecution.git
```

If you have already cloned the repository without the `--recurse-submodules` you can clone the submodules using
```bash
git submodule update --init --recursive
```

To run, have the Docker-engine running on your computer, and use of the launch-profiles available in `Properties/launchSettings.json`.

Good luck :)

## Demo video

(Probably already outdated. This is a PoC, so it changes all the time)

**Demo with new GUI and simple feedback**

https://github.com/AlexanderNorup/InteractiveCodeExecution/assets/5619812/0eec83e0-3df8-4105-8d27-45cf84d66a53

You might notice it dosn't remove the errors after I fixed them. As I made this video, I simply just had not fixed that yet.


**Demo with real world assignment**

https://github.com/AlexanderNorup/InteractiveCodeExecution/assets/5619812/4f919a55-6dab-47e3-9666-93aa74614b5b

This demo runs a real point-giving assignment from the VOP course @ SDU. 

Running inside this container: https://github.com/users/AlexanderNorup/packages/container/package/interactivecodeexecution%2Fvnc_java

**Demo with assignments and submissions**

https://github.com/AlexanderNorup/InteractiveCodeExecution/assets/5619812/bf84299e-6b42-4ccb-be9b-2bf0aaf618a6

**VNC Demo with keyboard and mouse events**

https://github.com/AlexanderNorup/InteractiveCodeExecution/assets/5619812/bb34674a-30ce-42e3-86ae-a8561b162068

**Demo with concurrent independent live-streamed sources:**

https://github.com/AlexanderNorup/InteractiveCodeExecution/assets/5619812/19322a6f-228f-4baf-b45b-44a50f1f3a64

**Old demo just running code:**

https://github.com/AlexanderNorup/InteractiveCodeExecution/assets/5619812/949122c5-c7ed-428a-bb91-0c209133a354
