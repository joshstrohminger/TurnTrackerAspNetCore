# Turn Tracker
A turn-tracking web app to play with ASP.NET core and Entity Framework core.

Check out the current progress at https://turntracker.azurewebsites.net/

I've added identity but haven't worried too much about the visuals or URLs. My goal is to get the site working on a basic level before making it pretty.

Registration is open but it should only be used for demos/testing in its current state.

## Tasks
The activities that are tracked.

### Participants
Each task has a collection of participants that are actively taking turns. These are the only users that will have their turns tracked.

### Turns
Each task has a collection of turns taken by users. Each turn is a row in a table so that a complete history is always available.

## Users
Users can only view/edit tasks where they are the owner or a participant. Currently any of the participants or owner can edit tasks and turns.

### Admin Role
The admin role allows a user to view/edit tasks, turns, and users. They can also change roles for all users except themselves.

## Libraries
- [jQuery](https://jquery.com/)

  This is required by Bootstrap but since it's there I use it for a few things.
  
- [Bootstrap 3](https://getbootstrap.com/)

  I like/know the bootstrap UI system.
  
- [Hangfire](http://hangfire.io/)

  This is used to run recurring background jobs.
