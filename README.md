# Interactive Code Execution

This project contains code relating to my ISA (Individual Student Activity) *"Interactive Code Execution in ScalableTeaching"*. 

The project runs from February 1st to June 30th.

Student: **Alexander Nørup**, alnoe20@student.sdu.dk

Supervisors:
- **Aisha Umair**: aiu@mmmi.sdu.dk
- **Miguel Enrique Campusano Araya**: mica@mmmi.sdu.dk

## Running the code

As of writing, this is just a PoC. Have the Docker-engine running on your computer, and use of the launch-profiles available in `Properties/launchSettings.json`.

Good luck :)

## Demo video

(Probably already outdated. This is a PoC, so it changes all the time)

https://github.com/AlexanderNorup/InteractiveCodeExecution/assets/5619812/949122c5-c7ed-428a-bb91-0c209133a354

The video shows running dotnet code and taking: `9724.30ms`. 

If I instead set the Exec-command to `dotnet run --project Project.csproj` and then don't use any build-command, the execution time comes down to `5432.30ms`. Still super slow for a "Hello World", but better.

Another improvement I will experiment with at some point is to re-use containers. But that brings it's own problems and security concerns.
