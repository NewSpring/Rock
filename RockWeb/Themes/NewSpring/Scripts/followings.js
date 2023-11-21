document.addEventListener("DOMContentLoaded", function (event) {
  const followingsApiRoute = "/api/Followings";
  const followingClass = "js-following";

  // TAG BUTTON INTERACTION
  let followButtons = document.querySelectorAll("[data-follow]");
  followButtons.forEach(function (followButton, i) {
    followButton.addEventListener("click", function (e) {
      e.preventDefault();

      const followButton = followButtons[i];

      toggleFollowing(followButton);
    });
  });

  // CREATE TOPIC RELATIONSHIP
  function toggleFollowing(button) {
    const isFollowing = button.classList.contains(followingClass);
    const followingPersonAliasId = button.dataset.follow.split(",")[0];
    const followingEntityTypeId = button.dataset.follow.split(",")[1];
    const followingEntityId = button.dataset.follow.split(",")[2];

    // UI State Handling
    if (isFollowing) {
      // UNFOLLOW
      button.classList.add("bg-gray-darker");
      button.classList.remove(followingClass);
      document.activeElement.blur();
    } else {
      // FOLLOW
      button.classList.remove("bg-gray-darker");
      button.classList.add(followingClass);
      document.activeElement.blur();
    }

    // API Request
    var myHeaders = new Headers();

    var requestOptions = {
      method: isFollowing ? "DELETE" : "POST",
      headers: myHeaders,
      redirect: "follow",
    };

    fetch(followingsApiRoute + "/" + followingEntityTypeId + "/" + followingEntityId, requestOptions)
      .then((response) => response.text())
      .then((result) => console.log(result))
      .catch((error) => console.log("error", error));
  }
});
