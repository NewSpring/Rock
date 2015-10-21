# Developing with Rock

> Currently running Rock is only supported on windows devices.

Setting up the NewSpring fork of Rock to run locally, develop core features and create new plugins is easy to do with some understanding of powershell and git.

The repository contains the fork from SparkDevNetwork's Rock Project. It is the main repository for core changes as well as non-plugin specific custom NewSpring code. This guide contains instructions for running core Rock, working on plugins, working on themes, and even merging code from Spark's latest stable code.

### Requirements

Running Rock locally requires the following tools and programs:

- git
- Visual Studio
- SQL

> In these instructions, this is the **bare bones** setup

Optional requirements that have been configured for an easier development process:

- node (0.10.34 and higher)
- normajs

> In these instructions, this is the **norma** setup

### Setup

**This is a one time operation**

The initial setup is takes a few steps and may take some time depending on your internet speeds as this project is very large.

#### Step 1

After setting up git on your computer, clone down this project to the directory of your choice:

```powershell
git clone https://github.com/NewSpring/Rock.git
```

This can take a few minutes...

#### Step 2

NewSpring relies on a few custom plugins as well to run our full project. There are a couple methods to install these:

**Bare bones**
At the same folder level as the rock project, install all of the external plugins that are used by NewSpring's install.

```powershell
git clone https://github.com/NewSpring/rock-apollos && ^
git clone https://github.com/NewSpring/rock-cybersource && ^
git clone https://github.com/NewSpring/rock-attended-checkin && ^
git clone https://github.com/NewSpring/rock-workflows
```

After these have installed you need to link them into the Rock project.

> Symlinking on windows requires admin access. Use Command Prompt in admin role (right click on command prompt and click run as admin). Make sure you are at the same directory you were at (where rock is copied)

```command
cd .\Rock
mklink /D cc.newspring.Cybersource ..\rock-cybersource\cc.newspring.CyberSource
mklink /D cc.newspring.Apollos ..\rock-apollos\cc.newspring.Apollos
mklink /D cc.newspring.AttendedCheckin ..\rock-attended-checkin\cc.newspring.AttendedCheckin
mklink /D cc.newspring.Workflows ..\rock-workflows\cc.newspring.Workflows
cd .\RockWeb\Plugins\cc_newspring
mklink /D WorkFlowAlert ..\..\..\rock-workflows\cc_newspring
mklink /D AttendedCheckin ..\..\..\rock-attended-checkin\cc_newspring
cd ..\..\Bin
mklink BCrypt.Net.dll ..\..\rock-apollos\bin\BCrypt.Net.dll
cd ..\..\..\
```

This will pull down all of the required plugins and symlink them into the correct places. (We are working on integrating this into a single script in the future)

Now we need to install the theme files and link them in as well (again in command prompt with admin rights)

```command
git clone https://github.com/NewSpring/rock-themes.git
cd .\Rock\RockWeb\Themes
mklink /D Fuse ..\..\rock-themes\Fuse
mklink /D KidSpring ..\..\rock-themes\KidSprint
mklink /D NewSpring ..\..\rock-themes\Newspring
mklink /D Workflows ..\..\rock-themes\Workflows
cd ..\..\
```

At this point you have everything you need to run the project, move on to the next step!

**Norma**

If you have norma installed (`npm i normajs -g`), you can use the assitant to do bring in the needed projects.

```command
cd .\Rock && npm i && norma build && cd ..\
```

Once Norma finishes the tasks, move on to the next step!

#### Step 3

Now that all of the plugins and themes are installed, we can open the project in Visual Studio and run the project (more info to come here...)


## Developing Plugins

**Bare bones**
Developing plugins is already configured, huzzah!.

**Norma**
If you did the norma setup, we need to clone down the plugin you want to work on and link it into the .remote folder in your Rock project. Lets say you wanted to work on rock-apollos (make sure you are in the folder where Rock is installed, not in the project)

```
git clone https://github.com/NewSpring/rock-apollos
RD /S .\Rock\.remote\apollos
cd .\Rock\.remote
mklink /D apollos ..\..\rock-apollos
```

Then while you are working on projects, run `norma` at the root of the Rock project and you can save files and Norma will put them where they need to be!

**Shared**
Cd to the plugin you want to work on in your shell tool, starting at the master branch, checkout a new feature-branch. Then write new code from the Rock project in Visual Studio (or an editor of your choice) and commit in the project you checked out the branch from (e.g. rock-apollos).

Setting up new plugins will be documented soon!


## Deployments

**So you want to deploy Rock you say....**

We currently support three remote environments for the Rock application. We have a development owned alpha environment, a co-owned beta environment, and an IT owned production environment. Each environment is controlled and deployed via the corresponding branches in this repository. This repo also handles upstream merges and from SparkNetwork.

**Okay thats cool and all, but how to do I deploy?**

Simply! Whenever a commit is pushed (or merged) to any of the following branches: master, beta, alpha, an automated build will be triggered using the Appveyor CI service. Once this build is done, if it is successful, a build will be staged for deployment. If the alpha branch is what you are working on, it will automatically get deployed to alpha-rock.newspring.cc and run any corresponding migrations. If you intended to deploy to beta or master, the build will be staged and IT alerted as they make the call to deploy to those environments.

**How do I test my plugin in an environment?**

Right now, each Rock branch will pull from its corresponding plugin branches (e.g. beta branch of Rock pulls from beta branches of plugins). Stage your plugin to its corresponding environment branch, then it will trigger a deploy of Rock from that same branch. For example, rock-apollos@alpha deploys rock@alpha which pulls in the alpha of all plugins.

**This is cool, what about updating Rock Core?**

This is pretty easy as well! We ONLY use the pre-release-alpha branch from Rock right now. Eventually production will pull from the latest stable release instead. To pull from Rock, first add the core repo to your upstream of your clone  git remote add upstream https://github.com/SparkDevNetwork/Rock.git . Then when you want to merge,  git fetch upstream && git merge upstream/pre-alpha-release . This will bring the latest from core into our fork. In the future this will be automated as well. Then just push and your branch will have the latest from core.

**I want to make something new!**

So cool! Can't wait to see what it is! To start, checkout the master branch of this project and pull to make sure you have the latest. Then  git checkout -b feature-<my-thing> . At this point you have a branch ready to work on that is up to date with the latest from master. Then you can PR your branch through the environments all the way to production. Huzzah!
