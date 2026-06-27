import java.io.FileInputStream
import java.util.Properties
import org.gradle.api.GradleException

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

val keystorePropertiesFile = rootProject.file("keystore.properties")
val keystoreProperties = Properties()
if (keystorePropertiesFile.exists()) {
    FileInputStream(keystorePropertiesFile).use { input ->
        keystoreProperties.load(input)
    }
}

fun hasReleaseSigning(): Boolean {
    val requiredKeys = listOf(
        "storeFile",
        "storePassword",
        "keyAlias",
        "keyPassword",
    )
    return requiredKeys.all { keystoreProperties.getProperty(it) != null }
}

android {
    namespace = "se.sesport.webwrapper"
    compileSdk = 34

    defaultConfig {
        applicationId = "se.sesport.webwrapper"
        minSdk = 24
        targetSdk = 34
        versionCode = 1
        versionName = "1.0"
    }

    signingConfigs {
        if (hasReleaseSigning()) {
            create("release") {
                storeFile =
                    rootProject.file(
                        keystoreProperties.getProperty("storeFile")
                    )
                storePassword =
                    keystoreProperties.getProperty("storePassword")
                keyAlias = keystoreProperties.getProperty("keyAlias")
                keyPassword =
                    keystoreProperties.getProperty("keyPassword")
            }
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile(
                    "proguard-android-optimize.txt"
                ),
                "proguard-rules.pro",
            )
            if (hasReleaseSigning()) {
                signingConfig = signingConfigs.getByName("release")
            }
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    implementation("androidx.activity:activity-ktx:1.9.2")
    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.core:core-splashscreen:1.0.1")
    implementation("com.google.android.material:material:1.12.0")
}

gradle.taskGraph.whenReady {
    val wantsRelease = allTasks.any {
        it.name.contains("Release", ignoreCase = true)
    }
    if (wantsRelease && !hasReleaseSigning()) {
        throw GradleException(
            "Missing keystore.properties for release signing."
        )
    }
}
