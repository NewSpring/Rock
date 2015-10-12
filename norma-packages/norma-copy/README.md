norma-copy
===========

File Copy Package for Norma Build Tool

To use add the following to your norma.json:

```javascript
"tasks": {
  "copy": {
    "src": "test/**/*",
    "dest": "out"
  }
}
```

The `src` variable is where your test files are located.

The `dest` variable is where you want your files to go

You can also specify what types of files you want copied by using the `ext` key like so:

```javascript
"tasks": {
  "copy": {
    "src": "test/**/*",
    "dest": "out",
    "ext": ["html", js]
  }
}
```
