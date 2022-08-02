using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Secrets/Login",fileName = "LoginToken.secret.asset")]
public class SLToken : ScriptableObject
{
    //Yeah; its not secure, it's for dev purposes, because retyping this EVERYTIME is annoying
    //Because this is a file; we can gitignore it; BUT YOU MUST MAKE SURE TO GITIGNORE IT
    public string FirstName;
    public string LastName;
    public string Password;

}
