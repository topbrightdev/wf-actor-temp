@Library('pipeline-library@release/1.0.0') _

pipeline {
    agent {
        node {
            label instanceManager.getInstance('linux')
        }
    }
    options {
        timeout(time: 60, unit: 'MINUTES')
        skipDefaultCheckout()
        timestamps()
        buildDiscarder(logRotator(numToKeepStr: '' + common.config.numToKeepStr,
                                  daysToKeepStr: '' + common.config.daysToKeepStr,
                                  artifactNumToKeepStr: '' + common.config.artifactNumToKeepStr,
                                  artifactDaysToKeepStr: '' + common.config.artifactDaysToKeepStr))
    }
    environment {
        PROJECT_KEY = "wof"
        REPO_NAME = "wof-actor-csharp"
        OUT = "bin"
        VERSION_FILE = "version.txt"
        versionText = "0.0.0"
        BUILD_CONFIG = "Release"
        MAC_RUNTIME = "osx-x64"
        WIN_RUNTIME = "win-x64"
        LINUX_RUNTIME = "linux-x64"
        DOTNET_TARGET = "netcoreapp3.1"
        DIST = "dist"
    }
    triggers { bitbucketPush() }
    stages {
    stage('Clone') {
      steps {
        echo 'Cloning..'
        dir ("${WORKSPACE}") {
          script {
            def target = "${REPO_NAME}"
            gitSSHCheckout {
              targetDir = target
              withMerge = false
              withWipeOut = true
              withBuildProperties = true
            }
			gitSSHCheckout {
              branch = "master"
              project = "devops/build-scripts"
              targetDir = "resources"
              withMerge = false
              withWipeOut = false
              shallowClone = true
              withWorkspace = false
            }
          }
        }
      }
    }
    stage('Build') {
      steps {
        echo 'Building...'
        

        dir ("${WORKSPACE}") {
          script {
            props = readProperties file: "build.properties"
			
             if (common.config.uploadableBranch) {
              versionText = "${props.VERSION}-${BUILD_NUMBER}"
             } else {
               versionText = "0.0.0-${BUILD_NUMBER}"
             }
            echo "Target Version = ${versionText}"
            
            dir("${REPO_NAME}")
            {
              echo 'installing dotnet ...'
              sh """ asdf install"""
              withCredentials([usernamePassword(credentialsId: 'jenkins-service-account', usernameVariable: 'USERNAME', passwordVariable: 'PASSWORD')]) {

                sourcesNeeded = false
                echo "check nuget sources"
                sourcesNeeded = sh(script: """ dotnet nuget list source | grep timeplay.com -q""", returnStatus: true )
                if (sourcesNeeded != 0) {
                  echo "Add nuget sources"
                  sh """ dotnet nuget add source https://artifactory.timeplay.com/artifactory/api/nuget/nuget-release-local -n streamsix_snapshot -u ${USERNAME} -p ${PASSWORD} --store-password-in-clear-text """
                  sh """ dotnet nuget add source https://artifactory.timeplay.com/artifactory/api/nuget/nuget-snapshot-local -n streamsix_local -u ${USERNAME} -p ${PASSWORD} --store-password-in-clear-text """
                } else {
                  echo "nuget sources have already been installed."
                }
                echo "Clearing local nuget packages..."
                // sh """ dotnet nuget disable source ships """
                sh """ dotnet nuget locals all --clear """

                dir("controller")
                {
                  sh "sed -i 's/0.0.0-LOCALHOST/${versionText}/' ./Config.cs"
                }

                dir("ms_standalone")
                {
                  echo "Building mac ${MAC_RUNTIME} executable..."
                  sh """ dotnet publish --configuration ${BUILD_CONFIG} --runtime ${MAC_RUNTIME} --framework ${DOTNET_TARGET} -p:PublishSingleFile=true  --self-contained true """
                  echo "Building windows ${WIN_RUNTIME} executable..."
                  sh """ dotnet publish --configuration ${BUILD_CONFIG} --runtime ${WIN_RUNTIME} --framework ${DOTNET_TARGET} -p:PublishSingleFile=true  --self-contained true """
                  echo "Building Linux ${LINUX_RUNTIME} executable..."
                  sh """ dotnet publish --configuration ${BUILD_CONFIG} --runtime ${LINUX_RUNTIME} --framework ${DOTNET_TARGET} -p:PublishSingleFile=true --self-contained true """
                }

                dir("sat_standalone")
                {
                  echo "Building mac ${MAC_RUNTIME} executable..."
                  sh """ dotnet publish --configuration ${BUILD_CONFIG} --runtime ${MAC_RUNTIME} --framework ${DOTNET_TARGET} -p:PublishSingleFile=true  --self-contained true """
                  echo "Building windows ${WIN_RUNTIME} executable..."
                  sh """ dotnet publish --configuration ${BUILD_CONFIG} --runtime ${WIN_RUNTIME} --framework ${DOTNET_TARGET} -p:PublishSingleFile=true  --self-contained true """
                  echo "Building Linux ${LINUX_RUNTIME} executable..."
                  sh """ dotnet publish --configuration ${BUILD_CONFIG} --runtime ${LINUX_RUNTIME} --framework ${DOTNET_TARGET} -p:PublishSingleFile=true --self-contained true """
                }
                
                echo "Build done. ${versionText}"                
              }
            }
          }
        }
      }
    }
    stage('Test') {
      steps {
        echo "Testing ${versionText} ... "
      }
    }
    stage('Publish to Artifactory') {
      steps {
        echo 'Copying components...'
        
        dir ("${WORKSPACE}/${REPO_NAME}") {
          script {

            if (common.config.uploadableBranch) {              

              dir("${DIST}")
              {
                echo "Created dist folder ${DIST}"
                sh """ mkdir -p ./${MAC_RUNTIME}"""
                sh """ mkdir -p ./${WIN_RUNTIME}"""
                sh """ mkdir -p ./${LINUX_RUNTIME}"""

                // sh """ cp ../ms_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${MAC_RUNTIME}/publish/log4net.config log4net.mothership.config"""
                sh """ cp ../ms_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${MAC_RUNTIME}/publish/Timeplay.WheelOfFortune.Actor.Mothership ./${MAC_RUNTIME}/Timeplay.WheelOfFortune.Actor.Mothership"""
                sh """ cp ../ms_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${WIN_RUNTIME}/publish/Timeplay.WheelOfFortune.Actor.Mothership.exe ./${WIN_RUNTIME}/Timeplay.WheelOfFortune.Actor.Mothership.exe"""
                sh """ cp ../ms_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${LINUX_RUNTIME}/publish/Timeplay.WheelOfFortune.Actor.Mothership ./${LINUX_RUNTIME}/Timeplay.WheelOfFortune.Actor.Mothership"""

                // sh """ cp ../sat_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${MAC_RUNTIME}/publish/log4net.config log4net.satellite.config"""
                sh """ cp ../sat_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${MAC_RUNTIME}/publish/Timeplay.WheelOfFortune.Actor.Satellite ./${MAC_RUNTIME}/Timeplay.WheelOfFortune.Actor.Satellite"""
                sh """ cp ../sat_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${WIN_RUNTIME}/publish/Timeplay.WheelOfFortune.Actor.Satellite.exe ./${WIN_RUNTIME}/Timeplay.WheelOfFortune.Actor.Satellite.exe"""
                sh """ cp ../sat_standalone/bin/${BUILD_CONFIG}/${DOTNET_TARGET}/${LINUX_RUNTIME}/publish/Timeplay.WheelOfFortune.Actor.Satellite ./${LINUX_RUNTIME}/Timeplay.WheelOfFortune.Actor.Satellite"""

                echo 'Publishing loose platforms executable...'
                artifactory.uploadArtifacts("(*.*)", "{1}", [".git/*"], "false")
              }
              
            } else {
              echo "INFO: Artifact upload for branch ${env.BRANCH_NAME} is not allowed. Please get artifacts from build workspace instead"
            }
          }
        }
      }
    }
  }
  post {
    success { echo "Build was successful" }
    unstable { echo "Build was unstable" }
    failure { echo "Build failed" }
    always {
      script {
        sendNotifications(currentBuild.result, "email, mattermost", "false", "#deployment-tools")
      }
      echo "ALWAYS"
    }
  }
}